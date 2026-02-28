using System.Text.Json;
using System.Text.Json.Serialization;
using Dlp.AgentCore.Schema;

namespace Dlp.AgentCore.Logging;

/// <summary>
/// Writes audit log entries as NDJSON (one JSON object per line).
/// NEVER logs raw text content — only signals, decisions, and metadata.
/// </summary>
public sealed class AuditLogger : IDisposable
{
    private readonly string _logDirectory;
    private StreamWriter? _writer;
    private string _currentDate = string.Empty;
    private readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public AuditLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void LogEvent(TelemetryEvent evt, PolicyDecision decision)
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            EventId = evt.EventId,
            EventType = evt.EventType.ToString(),
            Domain = evt.Domain,
            Url = evt.Url,
            TabId = evt.TabId,
            CorrelationId = evt.CorrelationId,
            TextLength = evt.TextSignals?.Length,
            Sha256Prefix = evt.TextSignals?.Sha256Prefix,
            Patterns = evt.TextSignals?.Patterns,
            FileName = evt.FileSignals?.FileName,
            FileExtension = evt.FileSignals?.Extension,
            FileMimeType = evt.FileSignals?.MimeType,
            FileSizeBytes = evt.FileSignals?.SizeBytes,
            Decision = decision.Decision.ToString(),
            PolicyId = decision.PolicyId,
            PolicyVersion = decision.PolicyVersion,
            DecisionReason = decision.DecisionReason
        };

        var json = JsonSerializer.Serialize(entry, JsonOpts);
        WriteLine(json);
    }

    private void WriteLine(string line)
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            if (_currentDate != today || _writer == null)
            {
                _writer?.Flush();
                _writer?.Dispose();
                var path = Path.Combine(_logDirectory, $"events-{today}.ndjson");
                var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(fs) { AutoFlush = true };
                _currentDate = today;
            }
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}

/// <summary>
/// Audit log entry — contains ONLY signals and decisions, NEVER raw content.
/// </summary>
internal sealed class AuditEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int TabId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    // Text signals only — no raw text
    public int? TextLength { get; set; }
    public string? Sha256Prefix { get; set; }
    public List<string>? Patterns { get; set; }

    // File signals only — no file content
    public string? FileName { get; set; }
    public string? FileExtension { get; set; }
    public string? FileMimeType { get; set; }
    public long? FileSizeBytes { get; set; }

    // Decision
    public string Decision { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public string DecisionReason { get; set; } = string.Empty;
}
