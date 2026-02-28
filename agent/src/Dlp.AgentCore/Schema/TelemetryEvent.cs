using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Schema;

public sealed class TelemetryEvent
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("event_type")]
    public EventType EventType { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("tab_id")]
    public int TabId { get; set; }

    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("text_signals")]
    public TextSignals? TextSignals { get; set; }

    [JsonPropertyName("file_signals")]
    public FileSignals? FileSignals { get; set; }
}
