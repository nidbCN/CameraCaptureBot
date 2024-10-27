using CameraCaptureBot.Core.Extensions;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

namespace CameraCaptureBot.Core.Codecs;

public class CodecBase(ILogger logger) : IDisposable
{
    protected unsafe AVCodecContext* EncoderCtx;

    protected readonly unsafe AVPacket* Packet = ffmpeg.av_packet_alloc();

    public unsafe Queue<byte[]> Encode(AVFrame* frame)
    {
        var linkedBuffer = new Queue<byte[]>(2);

        EncoderCtx->width = frame->width;
        EncoderCtx->height = frame->height;
        EncoderCtx->sample_aspect_ratio = frame->sample_aspect_ratio;

        var ret = ffmpeg.avcodec_send_frame(EncoderCtx, frame);
        if (ret < 0)
        {
            // handle send exceptions.
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
                message = "codec not opened, it is a decoder, or requires flush\n";
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.ENOMEM))
            {
                message = "failed to add packet to internal queue, or similar\n";
            }

            logger.LogError(exception, message);
            throw exception;
        }

        for (ret = SendPacket(); ret == 0 && Packet->size > 0; ret = SendPacket())
        {
            var buffer = new byte[Packet->size];
            Marshal.Copy((IntPtr)Packet->data, buffer, 0, Packet->size);
            linkedBuffer.Enqueue(buffer);
        }

        if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == 0)
        {
            logger.LogWarning("Encode completed with {num} packets.", linkedBuffer.Count);
        }
        else
        {
            // references:
            // * https://ffmpeg.org/doxygen/6.1/group__lavc__decoding.html#ga5b8eff59cf259747cf0b31563e38ded6
            var exception = new ApplicationException(
                FfMpegExtension.av_strerror(ret));
            var message = "Uncaught error occured during encoding.\n";

            if (ret == ffmpeg.AVERROR_EOF)
            {
                // > the encoder has been fully flushed, and there will be no more output packets
                // should not happen because nobody send flush frame.
                message =
                    "The encoder has been fully flushed, and there will be no more output packets.\n";
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
            {
                // > codec not opened, or it is a decoder
                // should not happen because codec has been opened correct in ctor.
                message = "Codec not opened, or it is a decoder.\n";
            }

            logger.LogError(exception, message);

            throw exception;
        }

        return linkedBuffer;
    }

    private unsafe int SendPacket()
    {
        ffmpeg.av_packet_unref(Packet);
        return ffmpeg.avcodec_receive_packet(EncoderCtx, Packet);
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