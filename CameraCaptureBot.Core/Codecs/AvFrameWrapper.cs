using FFmpeg.AutoGen.Abstractions;

namespace VideoStreamCaptureBot.Core.Codecs;

public class AvFrameWrapper(AVFrame frame)
{
    public AVFrame Value { get; } = frame;
}