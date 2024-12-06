using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace CameraCaptureBot.Impl.Tencent.Protocols;
public record Payload<T> where T : new()
{
    [JsonPropertyName("id")]
    public required string EventId { get; set; }

    [JsonPropertyName("op")]
    public required OperationCode OperationCode { get; set; }

    [JsonPropertyName("d")]
    public required T Content { get; set; }

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
