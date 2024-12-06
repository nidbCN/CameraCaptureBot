using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Options;
using VideoStreamCaptureBot.Core.Configs;
using VideoStreamCaptureBot.Core.Extensions;
using VideoStreamCaptureBot.Core.Utils;
using NotImplementedException = System.NotImplementedException;

namespace VideoStreamCaptureBot.Core.Services;

public class WebpExportService : IDisposable
{
    private readonly ILogger<WebpExportService> _logger;
    private readonly StreamOption _streamOption;

    private readonly unsafe AVCodecContext* _webpEncoderCtx;

    public WebpExportService(ILogger<WebpExportService> logger, IOptions<StreamOption> streamOptions)
    {
        _logger = logger;
        _streamOption = streamOptions.Value;
        #region 初始化图片编码器

        unsafe
        {
            _webpEncoderCtx = FfMpegUtils.CreateCodecCtx(AVCodecID.AV_CODEC_ID_WEBP, config =>
            {
                config.Value->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
                config.Value->gop_size = 1;
                config.Value->thread_count = (int)_streamOption.CodecThreads;
                config.Value->time_base = new() { den = 1, num = 1000 };
                config.Value->flags |= ffmpeg.AV_CODEC_FLAG_COPY_OPAQUE;
                //config.Value->width = StreamWidth;
                //config.Value->height = StreamHeight;

                //ffmpeg.av_opt_set(config.Value->priv_data, "lossless", "0", ffmpeg.AV_OPT_SEARCH_CHILDREN)
                //  .ThrowExceptionIfError();
                //ffmpeg.av_opt_set(config.Value->priv_data, "compression_level", "4", ffmpeg.AV_OPT_SEARCH_CHILDREN)
                //  .ThrowExceptionIfError();
                ffmpeg.av_opt_set(config.Value->priv_data, "quality", "80", ffmpeg.AV_OPT_SEARCH_CHILDREN)
                    .ThrowExceptionIfError();
                ffmpeg.av_opt_set(config.Value->priv_data, "preset", "photo", ffmpeg.AV_OPT_SEARCH_CHILDREN)
                    .ThrowExceptionIfError();
            });
        }

        #endregion
    }

    public unsafe MemoryStream OpenWebpStreamUnsafe(AVFrame* inputFrame)
    {
        throw new NotImplementedException();
    }


    public unsafe void Dispose()
    {
        var encoderCtx = _webpEncoderCtx;
        ffmpeg.avcodec_free_context(&encoderCtx);
    }
}
