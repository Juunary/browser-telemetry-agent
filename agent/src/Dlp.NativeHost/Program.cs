using System.Text.Json;
using Dlp.AgentCore;
using Dlp.AgentCore.Logging;
using Dlp.AgentCore.Policy;
using Dlp.AgentCore.Schema;

namespace Dlp.NativeHost;

class Program
{
    private static PolicyEngine? _policyEngine;
    private static AuditLogger? _auditLogger;
    static async Task Main(string[] args)
    {
        Console.Error.WriteLine("[DLP NativeHost] Started. Waiting for messages on stdin...");

        // Load policy from file next to the executable, or from agent/policy/
        var policyPath = FindPolicyFile();
        if (policyPath != null)
        {
            _policyEngine = PolicyEngine.LoadFromFile(policyPath);
            Console.Error.WriteLine($"[DLP NativeHost] Policy loaded from: {policyPath}");
        }
        else
        {
            Console.Error.WriteLine("[DLP NativeHost] WARNING: No policy.json found. Using default allow.");
        }

        // Initialize audit logger
        var logDir = FindLogDirectory();
        _auditLogger = new AuditLogger(logDir);
        Console.Error.WriteLine($"[DLP NativeHost] Audit logs: {logDir}");
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                JsonDocument? doc;
                try
                {
                    doc = await NativeMessaging.ReadMessageAsync(stdin, cts.Token);
                }
                catch (InvalidOperationException ex)
                {
                    Console.Error.WriteLine($"[DLP NativeHost] Bad message: {ex.Message}");
                    continue;
                }
                catch (EndOfStreamException)
                {
                    Console.Error.WriteLine("[DLP NativeHost] Unexpected EOF.");
                    break;
                }

                if (doc == null)
                {
                    Console.Error.WriteLine("[DLP NativeHost] EOF on stdin. Exiting.");
                    break;
                }

                using (doc)
                {
                    await HandleMessageAsync(doc, stdout, cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[DLP NativeHost] Cancelled. Shutting down.");
        }

        _auditLogger?.Dispose();
        Console.Error.WriteLine("[DLP NativeHost] Exited.");
    }

    static string FindLogDirectory()
    {
        var exeDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(exeDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "agent", "logs");
            if (Directory.Exists(Path.Combine(dir.FullName, "agent")))
                return candidate;
            dir = dir.Parent;
        }
        return Path.Combine(exeDir, "logs");
    }

    static string? FindPolicyFile()
    {
        // Check next to executable
        var exeDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(exeDir, "policy.json");
        if (File.Exists(candidate)) return candidate;

        // Check agent/policy/ relative to repo root
        var dir = new DirectoryInfo(exeDir);
        while (dir != null)
        {
            candidate = Path.Combine(dir.FullName, "policy", "policy.json");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir.FullName, "agent", "policy", "policy.json");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return null;
    }
    static async Task HandleMessageAsync(JsonDocument doc, Stream stdout, CancellationToken ct)
    {
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "event")
        {
            Console.Error.WriteLine("[DLP NativeHost] Ignoring non-event message.");
            return;
        }

        if (!root.TryGetProperty("payload", out var payloadProp))
        {
            Console.Error.WriteLine("[DLP NativeHost] Missing payload.");
            return;
        }

        TelemetryEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<TelemetryEvent>(payloadProp.GetRawText());
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[DLP NativeHost] Failed to parse event: {ex.Message}");
            return;
        }

        if (evt == null)
        {
            Console.Error.WriteLine("[DLP NativeHost] Null event after deserialization.");
            return;
        }

        Console.Error.WriteLine($"[DLP NativeHost] Event: {evt.EventType} from {evt.Domain}");

        PolicyDecision decision;
        if (_policyEngine != null)
        {
            decision = _policyEngine.Evaluate(evt);
        }
        else
        {
            decision = new PolicyDecision
            {
                EventId = evt.EventId,
                Decision = Decision.allow,
                PolicyId = "none",
                PolicyVersion = "0",
                DecisionReason = "No policy loaded — default allow"
            };
        }

        Console.Error.WriteLine($"[DLP NativeHost] Decision: {decision.Decision} — {decision.DecisionReason}");

        // Write audit log (no raw text — only signals and decisions)
        try
        {
            _auditLogger?.LogEvent(evt, decision);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DLP NativeHost] Audit log write failed: {ex.Message}");
        }

        var response = new
        {
            type = "decision",
            payload = decision
        };

        await NativeMessaging.WriteMessageAsync(stdout, response, ct);
    }
}
