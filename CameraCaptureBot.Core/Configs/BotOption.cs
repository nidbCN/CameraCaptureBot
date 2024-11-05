using Lagrange.Core.Common;

namespace CameraCaptureBot.Core.Configs;

public record BotOption
{
    public string KeyStoreFile { get; set; } = "keystore.json";
    public string DeviceInfoFile { get; set; } = "deviceInfo.json";

    public IList<uint>? AllowedGroups { get; set; } = null;
    public IList<uint>? AllowedFriends { get; set; } = null;
    public IList<uint> AdminAccounts { get; set; } = [];

    public NotificationConfig NotificationConfig { get; set; } = new();


    public BotConfig FrameworkConfig { get; set; } = new()
    {
        AutoReconnect = true,
        AutoReLogin = true,
        GetOptimumServer = true,
        Protocol = Protocols.Linux,
        UseIPv6Network = true,
    };
}

public record NotificationConfig
{
    public bool NotifyAdminOnException { get; set; } = true;
    public bool NotifyWebhookOnException { get; set; } = false;
    public bool NotifyAdminOnHeartbeat { get; set; } = false;
    public bool NotifyWebhookOnHeartbeat { get; set; } = false;
    public uint HeartbeatIntervalHour { get; set; } = 6;
    public Uri? WebhookUrl { get; set; }
    public IDictionary<string, string?>? WebhookHeaders { get; set; }
}
