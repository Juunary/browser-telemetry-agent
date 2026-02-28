using System.Text.Json;
using Dlp.AgentCore.Schema;
using Xunit;

namespace Dlp.AgentCore.Tests;

public class SchemaTests
{
    [Fact]
    public void TelemetryEvent_Deserializes_From_Extension_Json()
    {
        var json = """
        {
            "event_id": "evt_123",
            "timestamp": "2025-01-01T00:00:00Z",
            "event_type": "CLIPBOARD_PASTE",
            "url": "https://example.com",
            "domain": "example.com",
            "tab_id": 1,
            "correlation_id": "cor_example_123",
            "text_signals": {
                "length": 42,
                "sha256_prefix": "AQIDBA==",
                "patterns": ["CREDIT_CARD"]
            }
        }
        """;

        var evt = JsonSerializer.Deserialize<TelemetryEvent>(json);

        Assert.NotNull(evt);
        Assert.Equal("evt_123", evt!.EventId);
        Assert.Equal(EventType.CLIPBOARD_PASTE, evt.EventType);
        Assert.Equal("example.com", evt.Domain);
        Assert.NotNull(evt.TextSignals);
        Assert.Equal(42, evt.TextSignals!.Length);
        Assert.Contains("CREDIT_CARD", evt.TextSignals.Patterns);
    }
}
