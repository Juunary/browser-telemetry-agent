using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Schema;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Decision
{
    allow,
    warn,
    block
}

public sealed class PolicyDecision
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("decision")]
    public Decision Decision { get; set; }

    [JsonPropertyName("policy_id")]
    public string PolicyId { get; set; } = string.Empty;

    [JsonPropertyName("policy_version")]
    public string PolicyVersion { get; set; } = string.Empty;

    [JsonPropertyName("decision_reason")]
    public string DecisionReason { get; set; } = string.Empty;
}
