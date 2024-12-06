using System.Net;
using System.Text.Json.Serialization;

namespace VideoStreamCaptureBot.Impl.Tencent.Options;
public record TencentImplOption
{
    [JsonConverter(typeof(JsonIPAddressConverter))]
    public IPAddress ListenIpAddress { get; set; } = IPAddress.Any;
    public uint ListenPort { get; set; } = 5033;
    public string Route { get; set; } = "/";

    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
    public required string Token { get; set; }
}
