namespace CameraCaptureBot.Core.Configs;

public record StreamOption
{
    public required string FfmpegRoot { get; set; }

    public required Uri Url { get; set; }
    public uint ConnectTimeout { get; set; } = 1200;

    public uint CodecTimeout { get; set; } = 5000;
    public uint CodecThreads { get; set; } = 4;
}
