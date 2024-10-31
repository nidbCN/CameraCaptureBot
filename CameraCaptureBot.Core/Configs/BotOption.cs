using Lagrange.Core.Common;

namespace CameraCaptureBot.Core.Configs;

public record BotOption
{
    public string KeyStoreFile { get; set; } = "keystore.json";
    public string DeviceInfoFile { get; set; } = "deviceInfo.json";

    public IList<uint>? AllowedGroups { get; set; } = null;
    public IList<uint>? AllowedFriends { get; set; } = null;
    public IList<uint> AdminAccounts { get; set; } = [];
    public bool NotifyAdminOnException { get; set; } = true;

    public BotConfig FrameworkConfig { get; set; } = new()
    {
        AutoReconnect = true,
        AutoReLogin = true,
        GetOptimumServer = true,
        Protocol = Protocols.Linux,
        UseIPv6Network = true,
    };
}
