﻿using System.Text.Json.Serialization;

namespace VideoStreamCaptureBot.Impl.Tencent.Protocols.EventContents;

public record HttpCallbackVerify : IEventContent
{
    [JsonPropertyName("plain_token")]
    public required string PlainToken { get; set; }

    [JsonPropertyName("event_ts")]
    public required string EventTimespan { get; set; }
}
