using System.Text.Json;
using Dlp.AgentCore.Logging;
using Dlp.AgentCore.Schema;
using Xunit;

namespace Dlp.AgentCore.Tests;

public class AuditLoggerTests : IDisposable
{
    private readonly string _tempDir;

    public AuditLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dlp-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { /* best effort cleanup */ }
    }

    private static string[] ReadLogLines(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
            lines.Add(line);
        return lines.ToArray();
    }

    private static string ReadLogText(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }

    [Fact]
    public void LogEvent_Creates_Ndjson_File()
    {
        using var logger = new AuditLogger(_tempDir);

        var evt = new TelemetryEvent
        {
            EventId = "evt-1",
            Timestamp = "2025-01-01T00:00:00Z",
            EventType = EventType.CLIPBOARD_PASTE,
            Url = "https://example.com/page",
            Domain = "example.com",
            TabId = 1,
            CorrelationId = "cor-1",
            TextSignals = new TextSignals
            {
                Length = 42,
                Sha256Prefix = "AAAA",
                Patterns = new List<string> { "CREDIT_CARD" }
            }
        };

        var decision = new PolicyDecision
        {
            EventId = "evt-1",
            Decision = Decision.warn,
            PolicyId = "test",
            PolicyVersion = "1.0",
            DecisionReason = "test reason"
        };

        logger.LogEvent(evt, decision);

        var files = Directory.GetFiles(_tempDir, "*.ndjson");
        Assert.Single(files);

        var lines = ReadLogLines(files[0]);
        Assert.Single(lines);

        var doc = JsonDocument.Parse(lines[0]);
        var root = doc.RootElement;
        Assert.Equal("evt-1", root.GetProperty("event_id").GetString());
        Assert.Equal("warn", root.GetProperty("decision").GetString());
        Assert.Equal(42, root.GetProperty("text_length").GetInt32());
    }

    [Fact]
    public void LogEvent_Does_Not_Contain_Raw_Text()
    {
        using var logger = new AuditLogger(_tempDir);

        var evt = new TelemetryEvent
        {
            EventId = "evt-2",
            Timestamp = "2025-01-01T00:00:00Z",
            EventType = EventType.CLIPBOARD_PASTE,
            Url = "https://example.com",
            Domain = "example.com",
            TabId = 1,
            CorrelationId = "cor-2",
            TextSignals = new TextSignals
            {
                Length = 100,
                Sha256Prefix = "BBBB",
                Patterns = new List<string>()
            }
        };

        var decision = new PolicyDecision
        {
            EventId = "evt-2",
            Decision = Decision.allow,
            PolicyId = "test",
            PolicyVersion = "1.0",
            DecisionReason = "allowed"
        };

        logger.LogEvent(evt, decision);

        var files = Directory.GetFiles(_tempDir, "*.ndjson");
        var content = ReadLogText(files[0]);

        // Verify no raw text fields exist in the log
        Assert.DoesNotContain("\"raw\"", content);
        Assert.DoesNotContain("\"text\":", content);
        Assert.DoesNotContain("\"content\":", content);
        Assert.DoesNotContain("\"clipboard\":", content);
    }

    [Fact]
    public void LogEvent_Multiple_Events_Append()
    {
        using var logger = new AuditLogger(_tempDir);

        for (int i = 0; i < 5; i++)
        {
            var evt = new TelemetryEvent
            {
                EventId = $"evt-{i}",
                Timestamp = "2025-01-01T00:00:00Z",
                EventType = EventType.CLIPBOARD_PASTE,
                Url = "https://example.com",
                Domain = "example.com",
                TabId = 1,
                CorrelationId = $"cor-{i}"
            };

            var decision = new PolicyDecision
            {
                EventId = $"evt-{i}",
                Decision = Decision.allow,
                PolicyId = "test",
                PolicyVersion = "1.0",
                DecisionReason = "ok"
            };

            logger.LogEvent(evt, decision);
        }

        var files = Directory.GetFiles(_tempDir, "*.ndjson");
        var lines = ReadLogLines(files[0]);
        Assert.Equal(5, lines.Length);
    }
}
