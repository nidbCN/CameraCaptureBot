using VideoStreamCaptureBot.Core.Extensions;
using VideoStreamCaptureBot.Core.Utils;
using FFmpeg.AutoGen.Abstractions;

namespace VideoStreamCaptureBot.Core.Codecs;

public class FfmpegBmpEncoder : CodecBase
{
    public FfmpegBmpEncoder(ILogger<FfmpegBmpEncoder> logger, BinarySizeFormatter format) : base(logger, format)
    {
        unsafe
        {
            var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_BMP);
            EncoderCtx = ffmpeg.avcodec_alloc_context3(codec);

            EncoderCtx->width = 1920;
            EncoderCtx->height = 1080;

            EncoderCtx->time_base = new() { num = 1, den = 25 }; // 设置时间基准
            EncoderCtx->framerate = new() { num = 25, den = 1 };

            EncoderCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_RGB24;

            EncoderCtx->gop_size = 1;
            EncoderCtx->time_base = new() { den = 1, num = 1000 };

            EncoderCtx->flags |= ffmpeg.AV_CODEC_FLAG_COPY_OPAQUE;

            ffmpeg.avcodec_open2(EncoderCtx, codec, null)
                .ThrowExceptionIfError();
        }
    }

    public unsafe void ConvertToRGB24(AVFrame* frame)
    {

    }
}