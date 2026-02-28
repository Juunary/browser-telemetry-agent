using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Schema;

public sealed class TextSignals
{
    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("sha256_prefix")]
    public string Sha256Prefix { get; set; } = string.Empty;

    [JsonPropertyName("patterns")]
    public List<string> Patterns { get; set; } = new();
}
