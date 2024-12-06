using System.Text.Json;
using System.Text.Json.Serialization;
using VideoStreamCaptureBot.Impl.Tencent.Protocols.EventContents;

namespace VideoStreamCaptureBot.Impl.Tencent.Protocols.Serialization;
public class JsonEventContentConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert != typeof(Payload))
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // get operation code
        var codeVal = root
            .GetProperty(Payload.OperationCodeProp)
            .GetInt32();
        var code = (OperationCode)codeVal;

        // get content prop json
        var contentJson = root
            .GetProperty(Payload.EventContentProp)
            .GetRawText();

        // get object by operation code
        object? content = code switch
        {
            OperationCode.Dispatch => JsonSerializer
                .Deserialize<Dispatch>(contentJson, options),
            OperationCode.HttpCallbackAck => JsonSerializer
                .Deserialize<HttpCallbackAck>(contentJson, options),
            OperationCode.HttpCallbackVerify => JsonSerializer
                .Deserialize<HttpCallbackVerify>(contentJson, options),
            _ => throw new JsonException($"Unsupported event code: {code}")
        };

        return content;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Serialization is not supported in this converter.");
    }
}
