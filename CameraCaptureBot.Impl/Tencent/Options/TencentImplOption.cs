using System.Net;
using System.Text.Json.Serialization;

namespace CameraCaptureBot.Impl.Tencent.Options;
public record TencentImplOption
{
    [JsonConverter(typeof(JsonIPAddressConverter))]
    public required IPAddress ListenIpAddress { get; init; } = IPAddress.Any;
    public required uint ListenPort { get; set; } = 5033;
    public required string Route { get; set; } = "/";

    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
    public required string Token { get; set; }
}
