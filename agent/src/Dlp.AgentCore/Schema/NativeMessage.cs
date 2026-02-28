using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Schema;

public sealed class NativeMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }
}
