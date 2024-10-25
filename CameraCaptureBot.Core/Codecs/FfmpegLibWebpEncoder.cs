using CameraCaptureBot.Core.Extensions;
using FFmpeg.AutoGen;

namespace CameraCaptureBot.Core.Codecs;

public class FfmpegLibWebpEncoder : CodecBase
{

    public FfmpegLibWebpEncoder(ILogger<FfmpegLibWebpEncoder> logger) : base(logger)
    {
        unsafe
        {
            var codec = ffmpeg.avcodec_find_encoder_by_name("libwebp");
            EncoderCtx = ffmpeg.avcodec_alloc_context3(codec);

            EncoderCtx->time_base = new() { num = 1, den = 25 }; // 设置时间基准
            EncoderCtx->framerate = new() { num = 25, den = 1 };

            EncoderCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

            EncoderCtx->gop_size = 1;
            EncoderCtx->time_base = new() { den = 1, num = 1000 };

            EncoderCtx->flags |= ffmpeg.AV_CODEC_FLAG_COPY_OPAQUE;

            ffmpeg.av_opt_set(EncoderCtx->priv_data,
                    "preset", "photo", ffmpeg.AV_OPT_SEARCH_CHILDREN)
                .ThrowExceptionIfError();

            ffmpeg.avcodec_open2(EncoderCtx, codec, null)
                .ThrowExceptionIfError();
        }
    }

    public override void Dispose()
    {
        unsafe
        {
            ffmpeg.avcodec_close(EncoderCtx);
            var encoderCtx = EncoderCtx;
            ffmpeg.avcodec_free_context(&encoderCtx);
        }

        GC.SuppressFinalize(this);
        base.Dispose();
    }
}