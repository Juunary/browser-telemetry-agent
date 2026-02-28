using System.Text.Json.Serialization;

namespace Dlp.AgentCore.Schema;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    CLIPBOARD_COPY,
    CLIPBOARD_PASTE,
    FILE_UPLOAD_ATTEMPT,
    LLM_PROMPT_PASTE
}
