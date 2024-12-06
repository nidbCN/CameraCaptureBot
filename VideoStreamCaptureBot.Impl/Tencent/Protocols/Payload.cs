using System.Text.Json.Serialization;
using VideoStreamCaptureBot.Impl.Tencent.Protocols.Serialization;

namespace VideoStreamCaptureBot.Impl.Tencent.Protocols;

public record Payload
{
    [JsonIgnore]
    public const string OperationCodeProp = "op";

    [JsonIgnore]
    public const string EventContentProp = "d";

    [JsonPropertyName("id")]
    public required string EventId { get; set; }

    [JsonPropertyName(OperationCodeProp)]
    public required OperationCode OperationCode { get; set; }

    [JsonConverter(typeof(JsonEventContentConverter))]
    [JsonPropertyName(EventContentProp)]
    public required object EventContent { get; set; } = null!;

    [JsonPropertyName("s")]
    public required uint Sequence { get; set; }

    [JsonPropertyName("t")]
    public required string EventType { get; set; }
}

public enum OperationCode
{
    /// <summary>
    /// 服务端进行消息推送
    /// </summary>
    Dispatch = 0,

    /// <summary>
    /// 仅用于 http 回调模式的回包，代表机器人收到了平台推送的数据
    /// </summary>
    HttpCallbackAck = 12,

    /// <summary>
    /// 开放平台对机器人服务端进行验证
    /// </summary>
    HttpCallbackVerify = 13,
}
