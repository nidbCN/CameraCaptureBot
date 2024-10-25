using CameraCaptureBot.Core.Extensions;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

namespace CameraCaptureBot.Core.Codecs;

public class CodecBase(ILogger logger) : IDisposable
{
    protected readonly ILogger Logger = logger;
    protected unsafe AVCodecContext* EncoderCtx;

    protected readonly unsafe AVPacket* Packet = ffmpeg.av_packet_alloc();

    public unsafe Task<Queue<byte[]>> EncodeAsync(AVFrame* frame)
    {
        return EncodeAsync(frame, CancellationToken.None);
    }

    public unsafe Task<Queue<byte[]>> EncodeAsync(AVFrame* frame, CancellationToken cancellationToken)
    {
        var linkedBuffer = new Queue<byte[]>(2);

        EncoderCtx->width = frame->width;
        EncoderCtx->height = frame->height;
        EncoderCtx->sample_aspect_ratio = frame->sample_aspect_ratio;

        return Task.Run(() =>
        {
            var ret = ffmpeg.avcodec_send_frame(EncoderCtx, frame);

            if (ret < 0)
            {
                var exception = new ApplicationException(FfMpegExtension.av_strerror(ret));
                string? message = null;

                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    message =
                        "input is not accepted in the current state - user must read output with avcodec_receive_packet()" +
                        "(once all output is read, the packet should be resent, and the call will not fail with EAGAIN).\n";
                }
                else if (ret == ffmpeg.AVERROR_EOF)
                {
                    message =
                        "the encoder has been flushed, and no new frames can be sent to it\n";
                }
                else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
                {
                    message = "codec not opened, it is a decoder, or requires flush";
                }
                else if (ret == ffmpeg.AVERROR(ffmpeg.ENOMEM))
                {
                    message = "failed to add packet to internal queue, or similar";
                }

                Logger.LogError(exception, message);
                throw exception;
            }

            while (!cancellationToken.IsCancellationRequested && ret != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                ffmpeg.av_packet_unref(Packet);

                ret = ffmpeg.avcodec_receive_packet(EncoderCtx, Packet);

                if (ret == 0 || ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    // > output is not available in the current state - user must try to send input
                    // > should never happen during flushing
                    // should not happen when using libwebp encoder in first receive,
                    // but can be this value with multiple calls to receive,
                    // and it means all packet has been received.

                    var buffer = new byte[Packet->size];
                    Marshal.Copy((IntPtr)Packet->data, buffer, 0, Packet->size);
                    linkedBuffer.Enqueue(buffer);
                }
                else
                {
                    // references:
                    // * https://ffmpeg.org/doxygen/6.1/group__lavc__decoding.html#ga5b8eff59cf259747cf0b31563e38ded6
                    var exception = new ApplicationException(
                        FfMpegExtension.av_strerror(ret));

                    if (ret == ffmpeg.AVERROR_EOF)
                    {
                        // > the encoder has been fully flushed, and there will be no more output packets
                        // should not happen because nobody send flush frame.
                        Logger.LogError(exception,
                            "The encoder has been fully flushed, and there will be no more output packets.\n");
                    }
                    else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
                    {
                        // > codec not opened, or it is a decoder
                        // should not happen because codec has been opened correct in ctor.
                        Logger.LogError(exception, "Codec not opened, or it is a decoder.\n");
                    }
                    else
                    {
                        Logger.LogError(exception, "Error occured during encoding.\n");
                    }

                    throw exception;
                }
            }

            return linkedBuffer;
        }, cancellationToken);
    }

    public virtual void Dispose()
    {
        unsafe
        {
            ffmpeg.av_packet_unref(Packet);
            var packet = Packet;
            ffmpeg.av_packet_free(&packet);
        }

        GC.SuppressFinalize(this);
    }
}