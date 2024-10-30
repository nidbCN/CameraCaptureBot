using CameraCaptureBot.Core.Extensions;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using CameraCaptureBot.Core.Utils;

namespace CameraCaptureBot.Core.Codecs;

public class CodecBase(ILogger logger, BinarySizeFormatter binarySizeFormat) : IDisposable
{
    protected unsafe AVCodecContext* EncoderCtx;

    protected readonly unsafe AVPacket* Packet = ffmpeg.av_packet_alloc();

    public unsafe Queue<byte[]> Encode(AVFrame* frame)
    {
        using (logger.BeginScope(
                   $"{EncoderCtx->codec_id}@{(IntPtr)EncoderCtx:x16}.{nameof(Encode)}"))
        {
            var linkedBuffer = new Queue<byte[]>(2);

            EncoderCtx->width = frame->width;
            EncoderCtx->height = frame->height;
            EncoderCtx->sample_aspect_ratio = frame->sample_aspect_ratio;

            logger.LogDebug("Try send frame@{id:x16} to encoder.", (IntPtr)frame->metadata);

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
                    message = "failed to add packet to (ffmpeg-managed) internal queue, or similar\n";
                }

#pragma warning disable CA2254
                logger.LogError(exception, message);
#pragma warning restore CA2254
                throw exception;
            }

            logger.LogInformation("Success sent frame@{id:x16} to decoder.", (IntPtr)frame->metadata);
            logger.LogDebug("If there's no another usage, this frame can be release now.");
            logger.LogDebug("Try receive packet from decoder.");

            for (ret = ReceivePacket(); ret == 0 && Packet->size > 0; ret = ReceivePacket())
            {
                logger.LogInformation("Received packet[{pos}] from decoder, size:{size}.", Packet->pos, Packet->size.ToString(binarySizeFormat));
                var buffer = new byte[Packet->size];
                Marshal.Copy((IntPtr)Packet->data, buffer, 0, Packet->size);
                linkedBuffer.Enqueue(buffer);
            }

            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == 0)
            {
                logger.LogInformation("Received EAGAIN from encoder, Encode completed with {num} packets.", linkedBuffer.Count);
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

#pragma warning disable CA2254
                logger.LogError(exception, message);
#pragma warning restore CA2254

                throw exception;
            }

            return linkedBuffer;
        }
    }

    private unsafe int ReceivePacket()
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