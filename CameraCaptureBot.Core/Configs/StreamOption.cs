namespace CameraCaptureBot.Core.Configs;

public record StreamOption
{
    public required Uri Url { get; set; }
    public uint ConnectTimeout { get; set; } = 1200;
    public uint CodecTimeout { get; set; } = 5000;
    public uint KeyframeSearchMax { get; set; } = 60;
    public uint CodecThreads { get; set; } = 4;
    public required string FfmpegRoot { get; set; }
    public string? LogLevel { get; set; }
    public bool KeyFrameOnly { get; set; } = true;
}
