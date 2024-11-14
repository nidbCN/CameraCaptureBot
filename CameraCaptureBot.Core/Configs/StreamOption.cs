namespace CameraCaptureBot.Core.Configs;

public record StreamOption
{
    public string? FfmpegRoot { get; set; } = null;

    public required Uri Url { get; set; }
    public uint ConnectTimeout { get; set; } = 1200;

    public uint CodecTimeout { get; set; } = 5000;
    public uint CodecThreads { get; set; } = 4;
}
