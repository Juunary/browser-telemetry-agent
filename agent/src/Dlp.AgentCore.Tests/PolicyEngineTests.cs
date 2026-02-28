using Dlp.AgentCore.Policy;
using Dlp.AgentCore.Schema;
using Xunit;

namespace Dlp.AgentCore.Tests;

public class PolicyEngineTests
{
    private static PolicyEngine CreateEngine()
    {
        var config = new PolicyConfig
        {
            PolicyId = "test-policy",
            PolicyVersion = "1.0.0",
            Default = "allow",
            Exceptions = new List<PolicyException>
            {
                new()
                {
                    Id = "exc-internal",
                    Description = "Allow internal domains",
                    Conditions = new PolicyConditions
                    {
                        DomainIn = new List<string> { "internal.company.com" }
                    },
                    Decision = "allow"
                }
            },
            Rules = new List<PolicyRule>
            {
                new()
                {
                    Id = "rule-sensitive",
                    Priority = 100,
                    Description = "Warn on sensitive patterns",
                    Conditions = new PolicyConditions
                    {
                        EventTypeIn = new List<string> { "CLIPBOARD_PASTE" },
                        PatternsAny = new List<string> { "CREDIT_CARD", "KR_RRN" }
                    },
                    Decision = "warn",
                    Reason = "Sensitive data detected"
                },
                new()
                {
                    Id = "rule-large-paste",
                    Priority = 90,
                    Description = "Warn on large paste",
                    Conditions = new PolicyConditions
                    {
                        EventTypeIn = new List<string> { "CLIPBOARD_PASTE" },
                        TextLengthMin = 5000
                    },
                    Decision = "warn",
                    Reason = "Large paste detected"
                },
                new()
                {
                    Id = "rule-block-exe",
                    Priority = 100,
                    Description = "Block exe uploads",
                    Conditions = new PolicyConditions
                    {
                        EventTypeIn = new List<string> { "FILE_UPLOAD_ATTEMPT" },
                        FileExtensionIn = new List<string> { ".exe", ".bat" }
                    },
                    Decision = "block",
                    Reason = "Executable upload blocked"
                }
            }
        };

        return new PolicyEngine(config);
    }

    private static TelemetryEvent MakeEvent(
        EventType type = EventType.CLIPBOARD_PASTE,
        string domain = "example.com",
        List<string>? patterns = null,
        int textLength = 100,
        string? fileExtension = null)
    {
        return new TelemetryEvent
        {
            EventId = "test-evt",
            Timestamp = "2025-01-01T00:00:00Z",
            EventType = type,
            Url = $"https://{domain}/page",
            Domain = domain,
            TabId = 1,
            CorrelationId = "cor-test",
            TextSignals = new TextSignals
            {
                Length = textLength,
                Sha256Prefix = "AAAA",
                Patterns = patterns ?? new List<string>()
            },
            FileSignals = fileExtension != null ? new FileSignals
            {
                FileName = $"file{fileExtension}",
                Extension = fileExtension,
                MimeType = "application/octet-stream",
                SizeBytes = 1024
            } : null
        };
    }

    [Fact]
    public void Exception_Matches_Before_Rules()
    {
        var engine = CreateEngine();
        var evt = MakeEvent(domain: "internal.company.com", patterns: new() { "CREDIT_CARD" });
        var decision = engine.Evaluate(evt);

        Assert.Equal(Decision.allow, decision.Decision);
        Assert.Contains("exc-internal", decision.DecisionReason);
    }

    [Fact]
    public void Sensitive_Pattern_Returns_Warn()
    {
        var engine = CreateEngine();
        var evt = MakeEvent(patterns: new() { "CREDIT_CARD" });
        var decision = engine.Evaluate(evt);

        Assert.Equal(Decision.warn, decision.Decision);
        Assert.Contains("rule-sensitive", decision.DecisionReason);
    }

    [Fact]
    public void Large_Paste_Returns_Warn()
    {
        var engine = CreateEngine();
        var evt = MakeEvent(textLength: 6000);
        var decision = engine.Evaluate(evt);

        Assert.Equal(Decision.warn, decision.Decision);
        Assert.Contains("rule-large-paste", decision.DecisionReason);
    }

    [Fact]
    public void Exe_Upload_Returns_Block()
    {
        var engine = CreateEngine();
        var evt = MakeEvent(type: EventType.FILE_UPLOAD_ATTEMPT, fileExtension: ".exe");
        var decision = engine.Evaluate(evt);

        Assert.Equal(Decision.block, decision.Decision);
        Assert.Contains("rule-block-exe", decision.DecisionReason);
    }

    [Fact]
    public void Normal_Paste_Returns_Allow()
    {
        var engine = CreateEngine();
        var evt = MakeEvent();
        var decision = engine.Evaluate(evt);

        Assert.Equal(Decision.allow, decision.Decision);
        Assert.Contains("default", decision.DecisionReason);
    }

    [Fact]
    public void Decision_Contains_PolicyId_And_Version()
    {
        var engine = CreateEngine();
        var evt = MakeEvent(patterns: new() { "KR_RRN" });
        var decision = engine.Evaluate(evt);

        Assert.Equal("test-policy", decision.PolicyId);
        Assert.Equal("1.0.0", decision.PolicyVersion);
    }
}
