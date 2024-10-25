using FFmpeg.AutoGen;

namespace CameraCaptureBot.Core.Codecs;

public interface ICodec : IDisposable
{
    // frame 由外部传入，packet 由实例持有
    public unsafe byte[] EncodeUnsafe(AVFrame* frame);
    public unsafe Task<byte[]> EncodeAsync(AVFrame* frame);
    public unsafe Task<MemoryStream> OpenEncodeAsync(AVFrame* frame);

    // frame 由外部传入
    public unsafe void DecodeUnsafe(byte[] data, out AVFrame* frame);
    public unsafe Task DecodeAsync(byte[] data, out AVFrame* frame);
    public unsafe Task WriteDecodeAsync(Stream stream, out AVFrame* frame);
}