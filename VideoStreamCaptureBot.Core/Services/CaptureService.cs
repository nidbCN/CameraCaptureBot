﻿using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Options;
using VideoStreamCaptureBot.Core.Codecs;
using VideoStreamCaptureBot.Core.Configs;
using VideoStreamCaptureBot.Core.Extensions;
using VideoStreamCaptureBot.Core.Utils;

namespace VideoStreamCaptureBot.Core.Services;

public readonly unsafe struct AvCodecContextWrapper(AVCodecContext* ctx)
{
    public AVCodecContext* Value { get; } = ctx;
}

public sealed class CaptureService : IDisposable
{
    private readonly ILogger<CaptureService> _logger;
    private readonly IDisposable? _scoop;
    private readonly BinarySizeFormatter _formatter;
    private readonly StreamOption _streamOption;
    private readonly FfmpegLibWebpEncoder _encoder;

    private readonly unsafe AVCodecContext* _decoderCtx;

    private unsafe AVFormatContext* _inputFormatCtx;
    private readonly unsafe AVDictionary* _openOptions = null;

    private readonly unsafe AVFrame* _frame = ffmpeg.av_frame_alloc();
    private readonly unsafe AVFrame* _webpOutputFrame = ffmpeg.av_frame_alloc();
    private readonly unsafe AVPacket* _packet = ffmpeg.av_packet_alloc();

    private readonly int _streamIndex;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _codecCancellationToken;

    public byte[]? LastCapturedImage { get; private set; }
    public DateTime LastCaptureTime { get; private set; }

    public string StreamDecoderName { get; }
    public AVPixelFormat StreamPixelFormat { get; }
    public int StreamHeight { get; private set; }
    public int StreamWidth { get; private set; }

    #region 创建编码器
    /// <summary>
    /// 创建编解码器
    /// </summary>
    /// <param name="codecId">编解码器ID</param>
    /// <param name="config">配置</param>
    /// <param name="pixelFormat">像素格式</param>
    /// <returns>编解码器上下文</returns>
    public static unsafe AVCodecContext* CreateCodecCtx(AVCodecID codecId, Action<AvCodecContextWrapper>? config = null, AVPixelFormat? pixelFormat = null)
    {
        var codec = ffmpeg.avcodec_find_encoder(codecId);

        return CreateCodecCtx(codec, ctx =>
        {
            ctx.Value->pix_fmt = pixelFormat ?? codec->pix_fmts[0];
            config?.Invoke(ctx);
        });
    }

    public static unsafe AVCodecContext* CreateCodecCtx(string codecName, Action<AvCodecContextWrapper>? config = null, AVPixelFormat? pixelFormat = null)
    {
        var codec = ffmpeg.avcodec_find_encoder_by_name(codecName);

        return CreateCodecCtx(codec, innerConfig =>
        {
            innerConfig.Value->pix_fmt = pixelFormat ?? codec->pix_fmts[0];
            config?.Invoke(innerConfig);
        });
    }

    public static unsafe AVCodecContext* CreateCodecCtx(AVCodec* codec, Action<AvCodecContextWrapper>? config = null)
    {
        // 编解码器
        var ctx = ffmpeg.avcodec_alloc_context3(codec);

        ctx->time_base = new() { num = 1, den = 25 }; // 设置时间基准
        ctx->framerate = new() { num = 25, den = 1 };

        config?.Invoke(new AvCodecContextWrapper(ctx));

        ffmpeg.avcodec_open2(ctx, codec, null).ThrowExceptionIfError();

        return ctx;
    }
    #endregion

    public unsafe CaptureService(ILogger<CaptureService> logger, IOptions<StreamOption> option, FfmpegLibWebpEncoder encoder, BinarySizeFormatter formatter)
    {
        _logger = logger;
        _streamOption = option.Value;
        _encoder = encoder;
        _formatter = formatter;

        _scoop = logger.BeginScope(nameof(CaptureService));

        if (_streamOption.Url is null)
            throw new ArgumentNullException(nameof(option), "StreamOption.Url can not be null.");

        _codecCancellationToken = new(TimeSpan.FromMilliseconds(
            _streamOption.CodecTimeout));

        // 设置超时
        var openOptions = _openOptions;
        ffmpeg.av_dict_set(&openOptions, "timeout", _streamOption.ConnectTimeout.ToString(), 0);

        #region 初始化视频流解码器
        OpenInput();

        ffmpeg.avformat_find_stream_info(_inputFormatCtx, null)
            .ThrowExceptionIfError();

        // 匹配解码器信息
        AVCodec* decoder = null;
        _streamIndex = ffmpeg
            .av_find_best_stream(_inputFormatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &decoder, 0)
            .ThrowExceptionIfError();

        // 创建解码器
        _decoderCtx = CreateCodecCtx(decoder, codec =>
        {
            ffmpeg.avcodec_parameters_to_context(codec.Value, _inputFormatCtx->streams[_streamIndex]->codecpar)
                .ThrowExceptionIfError();

            codec.Value->thread_count = (int)_streamOption.CodecThreads;
            codec.Value->flags |= ffmpeg.AV_CODEC_FLAG_LOW_DELAY;
            codec.Value->skip_frame = AVDiscard.AVDISCARD_NONKEY;
        });

        var pixFormat = _decoderCtx->pix_fmt switch
        {
            AVPixelFormat.AV_PIX_FMT_YUVJ420P => AVPixelFormat.AV_PIX_FMT_YUV420P,
            AVPixelFormat.AV_PIX_FMT_YUVJ422P => AVPixelFormat.AV_PIX_FMT_YUV422P,
            AVPixelFormat.AV_PIX_FMT_YUVJ444P => AVPixelFormat.AV_PIX_FMT_YUV444P,
            AVPixelFormat.AV_PIX_FMT_YUVJ440P => AVPixelFormat.AV_PIX_FMT_YUV440P,
            _ => _decoderCtx->pix_fmt,
        };

        // 设置输入流信息
        StreamDecoderName = ffmpeg.avcodec_get_name(decoder->id);
        StreamPixelFormat = pixFormat;
        StreamWidth = _decoderCtx->width;
        StreamHeight = _decoderCtx->height;

        CloseInput();
        #endregion
    }

    private unsafe void OpenInput()
    {
        _logger.LogDebug("Open Input {url}.", _streamOption.Url.AbsoluteUri);

        _inputFormatCtx = ffmpeg.avformat_alloc_context();
        var formatCtx = _inputFormatCtx;

        // 设置超时
        var openOptions = _openOptions;

        // 打开流
        ffmpeg.avformat_open_input(&formatCtx, _streamOption.Url.AbsoluteUri, null, &openOptions)
            .ThrowExceptionIfError();
    }

    private unsafe void CloseInput()
    {
        _logger.LogDebug("Close Input.");

        var formatCtx = _inputFormatCtx;
        ffmpeg.avformat_close_input(&formatCtx);
        ffmpeg.avformat_free_context(formatCtx);
    }

    // 会引发异常，待排查
    public unsafe void Dispose()
    {
        var pFrame = _frame;
        ffmpeg.av_frame_free(&pFrame);
        var pWebpOutputFrame = _webpOutputFrame;
        ffmpeg.av_frame_free(&pWebpOutputFrame);
        var pPacket = _packet;
        ffmpeg.av_packet_free(&pPacket);

        _scoop?.Dispose();
    }

    private TimeSpan FfmpegTimeToTimeSpan(long value, AVRational timebase)
    {
        if (timebase.den == 0)
        {
            timebase.num = 1;
            timebase.den = ffmpeg.AV_TIME_BASE;
            _logger.LogWarning("Timebase den not set, reset to {num}/{den}",
                timebase.num, timebase.den);
        }

        var milliseconds = (double)(value * timebase.num) / ((long)timebase.den * 1000);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// 解码下一关键帧（非线程安全）
    /// </summary>
    /// <param name="frame">帧指针，指向_frame或null</param>
    /// <returns></returns>
    public unsafe AVFrame* DecodeNextFrameUnsafe()
    {
        using (_logger.BeginScope($"{ffmpeg.avcodec_get_name(_decoderCtx->codec_id)}@0x{(IntPtr)_decoderCtx:x16}"))
        {
            IDisposable? scope = null;
            var decodeResult = -1;
            var timeoutTokenSource = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(_streamOption.CodecTimeout));

            while (!timeoutTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    do
                    {
                        // 遍历流查找 bestStream
                        ffmpeg.av_packet_unref(_packet);
                        var readResult = ffmpeg.av_read_frame(_inputFormatCtx, _packet);

                        // 视频流已经结束
                        if (readResult == ffmpeg.AVERROR_EOF)
                        {
                            var error = new ApplicationException(FfMpegExtension.av_strerror(readResult));
                            const string message = "Receive EOF in stream.\n";

                            _logger.LogError(error, message);
                            throw new EndOfStreamException(message, error);
                        }

                        // 其他错误
                        readResult.ThrowExceptionIfError();
                    } while (_packet->stream_index != _streamIndex);

                    scope?.Dispose();
                    scope = _logger.BeginScope($"Packet@0x{_packet->buf->GetHashCode():x8}");

                    // 取到了 stream 中的包
                    _logger.LogInformation(
                        "Find packet in stream {index}, size:{size}, pts(display):{pts}, dts(decode):{dts}, key frame flag:{containsKey}",
                        _packet->stream_index,
                        string.Format(_formatter, "{0}", _packet->size),
                        FfmpegTimeToTimeSpan(_packet->pts, _decoderCtx->time_base).ToString("c"),
                        FfmpegTimeToTimeSpan(_packet->dts, _decoderCtx->time_base).ToString("c"),
                        (_packet->flags & ffmpeg.AV_PKT_FLAG_KEY) == 1 ? ffmpeg.AV_PKT_FLAG_KEY.ToString() : "NO_FLAG"
                    );

                    // 空包
                    if (_packet->size <= 0)
                    {
                        _logger.LogWarning("Packet with invalid size {size}, ignore.",
                            string.Format(_formatter, "{0}", _packet->size));
                    }

                    // 校验关键帧
                    if ((_packet->flags & ffmpeg.AV_PKT_FLAG_KEY) == 0x00)
                    {
                        _logger.LogInformation("Packet not contains KEY frame, drop.");
                        continue;
                    }

                    // 校验 PTS
                    if (_packet->pts < 0)
                    {
                        _logger.LogWarning("Packet pts={pts} < 0, drop.",
                            FfmpegTimeToTimeSpan(_packet->pts, _decoderCtx->time_base).ToString("c"));
                        continue;
                    }

                    // 尝试发送
                    _logger.LogDebug("Try send packet to decoder.");
                    var sendResult = ffmpeg.avcodec_send_packet(_decoderCtx, _packet);

                    if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                    {
                        // reference:
                        // * tree/release/6.1/fftools/ffmpeg_dec.c:567
                        // 理论上不会出现 EAGAIN

                        _logger.LogWarning(
                            "Receive {error} after sent, this could be cause by ffmpeg bug or some reason, ignored this message.",
                            nameof(ffmpeg.EAGAIN));
                        sendResult = 0;
                    }

                    if (sendResult == 0 || sendResult == ffmpeg.AVERROR_EOF)
                    {
                        // 发送成功
                        _logger.LogDebug("Packet sent success, try get decoded frame.");
                        // 获取解码结果
                        decodeResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _frame);
                    }
                    else
                    {
                        var error = new ApplicationException(FfMpegExtension.av_strerror(sendResult));

                        // 无法处理的发送失败
                        _logger.LogError(error, "Send packet to decoder failed.\n");

                        throw error;
                    }

                    scope?.Dispose();
                    scope = _logger.BeginScope($"Frame@0x{_frame->GetHashCode():x8}");

                    if (decodeResult < 0)
                    {
                        // 错误处理
                        ApplicationException error;
                        var message = FfMpegExtension.av_strerror(decodeResult);

                        if (decodeResult == ffmpeg.AVERROR_EOF)
                        {
                            // reference:
                            // * https://ffmpeg.org/doxygen/6.1/group__lavc__decoding.html#ga11e6542c4e66d3028668788a1a74217c
                            // > the codec has been fully flushed, and there will be no more output frames
                            // 理论上不会出现 EOF
                            message =
                                "the codec has been fully flushed, and there will be no more output frames.";

                            error = new(message);

                            _logger.LogError(error, "Received EOF from decoder.\n");
                        }
                        else if (decodeResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        {
                            // reference:
                            // * tree/release/6.1/fftools/ffmpeg_dec.c:596
                            // * https://ffmpeg.org/doxygen/6.1/group__lavc__decoding.html#ga11e6542c4e66d3028668788a1a74217c
                            // > output is not available in this state - user must try to send new input
                            // 理论上不会出现 EAGAIN
                            message =
                                "output is not available in this state - user must try to send new input";

                            //if (_streamOption.KeyFrameOnly)
                            //{
                            //    // 抛出异常，仅关键帧模式中，该错误不可能通过发送更多需要的包来解决
                            //    error = new(message);

                            //    _logger.LogError(error, "Received EAGAIN from decoder.\n");
                            //    throw error;
                            //}

                            // 忽略错误，发送下一个包进行编码，可能足够的包进入解码器可以解决
                            _logger.LogWarning("Receive EAGAIN from decoder, retry.");
                            continue;
                        }
                        else
                        {
                            error = new(message);
                            _logger.LogError(error, "Uncaught error occured during decoding.\n");
                            throw error;
                        }
                    }

                    // 解码正常
                    _logger.LogInformation("Decode frame success. type {type}, pts {pts}.",
                        _frame->pict_type.ToString(),
                        FfmpegTimeToTimeSpan(_frame->pts, _decoderCtx->time_base).ToString("c"));

                    scope?.Dispose();

                    break;
                }
                finally
                {
                    ffmpeg.av_packet_unref(_packet);
                }
            }

            if (decodeResult != 0)
            {
                // 解码失败
                var error = new TaskCanceledException("Decode timeout.");
                _logger.LogError(error, "Failed to decode.\n");
                throw error;
            }

            if (_decoderCtx->hw_device_ctx is not null)
            {
                _logger.LogError("Hardware decode is unsupported, skip.");
                // 硬件解码数据转换
                // ffmpeg.av_hwframe_transfer_data(frame, _frame, 0).ThrowExceptionIfError();
            }

            return _frame;
        }
    }

    /// <summary>
    /// 丢弃解码器结果中所有的帧（异步）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task FlushDecoderBufferAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await Task.Run(FlushDecoderBufferUnsafe, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 丢弃解码器结果中所有的帧
    /// </summary>
    private unsafe void FlushDecoderBufferUnsafe()
    {
        var cnt = 0;
        int result;
        do
        {
            result = ffmpeg.avcodec_receive_frame(_decoderCtx, _frame);
            _logger.LogDebug("Drop frame[{seq}] in decoder queue[{num}] in decoder buffer.", cnt, _decoderCtx->frame_num);
            cnt++;
        } while (result != ffmpeg.AVERROR(ffmpeg.EAGAIN));

        ffmpeg.av_frame_unref(_frame);
        _logger.LogInformation("Drop all {cnt} frames in decoder buffer.", cnt);
    }

    /// <summary>
    /// 创建转换上下文
    /// </summary>
    /// <param name="targetCodecCtx"></param>
    /// <param name="targetWidth"></param>
    /// <param name="targetHeight"></param>
    /// <returns></returns>
    private unsafe SwsContext* CreateSwsContext(AVCodecContext* targetCodecCtx, int targetWidth, int targetHeight)
        => ffmpeg.sws_getContext(StreamWidth, StreamHeight, StreamPixelFormat,
            targetWidth, targetHeight, targetCodecCtx->pix_fmt,
            ffmpeg.SWS_BICUBIC, null, null, null);

    /// <summary>
    /// 截取图片（异步）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>是否成功，图片字节码</returns>
    public async Task<(bool, byte[]?)> CaptureImageAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        // Check image cache.
        try
        {
            var captureTimeSpan = DateTime.Now - LastCaptureTime;
            if (LastCapturedImage != null && captureTimeSpan <= TimeSpan.FromSeconds(5))
            {
                _logger.LogInformation("Return image cached {time} ago.", captureTimeSpan);
                return (true, LastCapturedImage);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Capture new image and process it.
        var result = await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Cache image expired, capture new.");
            try
            {
                unsafe
                {
                    OpenInput();
                    var decodedFrame = DecodeNextFrameUnsafe();
                    CloseInput();

                    var queue = _encoder.Encode(decodedFrame);
                    if (queue.Count > 1)
                    {
                        var bufferSize = queue.Sum(buf => buf.Length);
                        var buffer = new byte[bufferSize];

                        var copied = 0;
                        foreach (var bufferBlock in queue)
                        {
                            Buffer.BlockCopy(bufferBlock, 0, buffer, copied, bufferBlock.Length);
                            copied += bufferBlock.Length;
                        }

                        LastCapturedImage = buffer;
                    }
                    else
                    {
                        LastCapturedImage = queue.Dequeue();
                    }

                    LastCaptureTime = DateTime.Now;
                    return (true, LastCapturedImage);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }, cancellationToken);
        return result;
    }
}