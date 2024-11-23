using FFmpeg.AutoGen.Abstractions;

namespace CameraCaptureBot.Core.Codecs;

public class AvFrameWrapper(AVFrame frame)
{
    public AVFrame Value { get; } = frame;
}