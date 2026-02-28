using System.Text.Json;
using Dlp.AgentCore.Schema;

namespace Dlp.AgentCore.Policy;

/// <summary>
/// Policy Decision Point (PDP). Evaluates events against policy.json rules.
/// Order: exceptions first, then rules by priority descending, then default.
/// </summary>
public sealed class PolicyEngine
{
    private readonly PolicyConfig _config;

    public PolicyEngine(PolicyConfig config)
    {
        _config = config;
    }

    public static PolicyEngine LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<PolicyConfig>(json)
            ?? throw new InvalidOperationException("Failed to deserialize policy.json");
        return new PolicyEngine(config);
    }

    public PolicyDecision Evaluate(TelemetryEvent evt)
    {
        // 1. Check exceptions first
        foreach (var exc in _config.Exceptions)
        {
            if (MatchesConditions(exc.Conditions, evt))
            {
                return MakeDecision(evt, exc.Decision, exc.Id, exc.Description);
            }
        }

        // 2. Evaluate rules by priority descending
        var sortedRules = _config.Rules.OrderByDescending(r => r.Priority);
        foreach (var rule in sortedRules)
        {
            if (MatchesConditions(rule.Conditions, evt))
            {
                return MakeDecision(evt, rule.Decision, rule.Id, rule.Reason);
            }
        }

        // 3. Default
        return MakeDecision(evt, _config.Default, "default", "No matching rule — default policy applied");
    }

    private PolicyDecision MakeDecision(TelemetryEvent evt, string decision, string ruleId, string reason)
    {
        var d = decision.ToLowerInvariant() switch
        {
            "warn" => Decision.warn,
            "block" => Decision.block,
            _ => Decision.allow,
        };

        return new PolicyDecision
        {
            EventId = evt.EventId,
            Decision = d,
            PolicyId = _config.PolicyId,
            PolicyVersion = _config.PolicyVersion,
            DecisionReason = $"[{ruleId}] {reason}"
        };
    }

    private static bool MatchesConditions(PolicyConditions cond, TelemetryEvent evt)
    {
        // All specified conditions must match (AND logic)

        if (cond.EventTypeIn != null && cond.EventTypeIn.Count > 0)
        {
            if (!cond.EventTypeIn.Contains(evt.EventType.ToString()))
                return false;
        }

        if (cond.DomainIn != null && cond.DomainIn.Count > 0)
        {
            if (!cond.DomainIn.Any(d => string.Equals(d, evt.Domain, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (cond.DomainNotIn != null && cond.DomainNotIn.Count > 0)
        {
            if (cond.DomainNotIn.Any(d => string.Equals(d, evt.Domain, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        if (cond.PatternsAny != null && cond.PatternsAny.Count > 0)
        {
            var evtPatterns = evt.TextSignals?.Patterns ?? new List<string>();
            if (!cond.PatternsAny.Any(p => evtPatterns.Contains(p)))
                return false;
        }

        if (cond.TextLengthMin.HasValue)
        {
            var length = evt.TextSignals?.Length ?? 0;
            if (length < cond.TextLengthMin.Value)
                return false;
        }

        if (cond.FileExtensionIn != null && cond.FileExtensionIn.Count > 0)
        {
            var ext = evt.FileSignals?.Extension ?? string.Empty;
            if (!cond.FileExtensionIn.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}
