using System.Text;
using System.Text.Json;

namespace Dlp.AgentCore;

/// <summary>
/// Chrome native messaging framing: 4-byte little-endian length prefix + UTF-8 JSON.
/// </summary>
public static class NativeMessaging
{
    /// <summary>Maximum allowed message size (1 MB). Chrome's limit is also ~1 MB.</summary>
    public const int MaxMessageSize = 1024 * 1024;

    /// <summary>
    /// Read one native message frame from a stream.
    /// Returns null on EOF or if the stream is closed.
    /// </summary>
    public static async Task<JsonDocument?> ReadMessageAsync(Stream input, CancellationToken ct = default)
    {
        var lengthBuf = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int n = await input.ReadAsync(lengthBuf.AsMemory(bytesRead, 4 - bytesRead), ct);
            if (n == 0) return null; // EOF
            bytesRead += n;
        }

        int length = BitConverter.ToInt32(lengthBuf, 0);

        if (length <= 0 || length > MaxMessageSize)
        {
            throw new InvalidOperationException($"Invalid message length: {length}");
        }

        var msgBuf = new byte[length];
        bytesRead = 0;
        while (bytesRead < length)
        {
            int n = await input.ReadAsync(msgBuf.AsMemory(bytesRead, length - bytesRead), ct);
            if (n == 0) throw new EndOfStreamException("Unexpected EOF while reading message body");
            bytesRead += n;
        }

        return JsonDocument.Parse(msgBuf);
    }

    /// <summary>
    /// Write one native message frame to a stream.
    /// </summary>
    public static async Task WriteMessageAsync(Stream output, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        if (json.Length > MaxMessageSize)
        {
            throw new InvalidOperationException($"Message too large: {json.Length} bytes");
        }

        var lengthBytes = BitConverter.GetBytes(json.Length);
        await output.WriteAsync(lengthBytes, ct);
        await output.WriteAsync(json, ct);
        await output.FlushAsync(ct);
    }
}
