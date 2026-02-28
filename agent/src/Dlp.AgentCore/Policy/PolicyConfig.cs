using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Policy;

public sealed class PolicyConfig
{
    [JsonPropertyName("policy_id")]
    public string PolicyId { get; set; } = string.Empty;

    [JsonPropertyName("policy_version")]
    public string PolicyVersion { get; set; } = string.Empty;

    [JsonPropertyName("default")]
    public string Default { get; set; } = "allow";

    [JsonPropertyName("exceptions")]
    public List<PolicyException> Exceptions { get; set; } = new();

    [JsonPropertyName("rules")]
    public List<PolicyRule> Rules { get; set; } = new();
}

public sealed class PolicyException
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("conditions")]
    public PolicyConditions Conditions { get; set; } = new();

    [JsonPropertyName("decision")]
    public string Decision { get; set; } = "allow";
}

public sealed class PolicyRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("conditions")]
    public PolicyConditions Conditions { get; set; } = new();

    [JsonPropertyName("decision")]
    public string Decision { get; set; } = "allow";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class PolicyConditions
{
    [JsonPropertyName("event_type_in")]
    public List<string>? EventTypeIn { get; set; }

    [JsonPropertyName("domain_in")]
    public List<string>? DomainIn { get; set; }

    [JsonPropertyName("domain_not_in")]
    public List<string>? DomainNotIn { get; set; }

    [JsonPropertyName("patterns_any")]
    public List<string>? PatternsAny { get; set; }

    [JsonPropertyName("text_length_min")]
    public int? TextLengthMin { get; set; }

    [JsonPropertyName("file_extension_in")]
    public List<string>? FileExtensionIn { get; set; }
}
