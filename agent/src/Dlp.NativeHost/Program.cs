using System.Text.Json;
using Dlp.AgentCore;
using Dlp.AgentCore.Schema;

namespace Dlp.NativeHost;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Error.WriteLine("[DLP NativeHost] Started. Waiting for messages on stdin...");

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

        Console.Error.WriteLine("[DLP NativeHost] Exited.");
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

        // For now, return a simple allow decision (PDP will be added in Issue 11)
        var decision = new PolicyDecision
        {
            EventId = evt.EventId,
            Decision = Decision.allow,
            PolicyId = "default",
            PolicyVersion = "1.0.0",
            DecisionReason = "Default allow (PDP not yet implemented)"
        };

        var response = new
        {
            type = "decision",
            payload = decision
        };

        await NativeMessaging.WriteMessageAsync(stdout, response, ct);
    }
}
