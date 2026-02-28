using System.Text;
using System.Text.Json;
using Dlp.AgentCore;
using Xunit;

namespace Dlp.AgentCore.Tests;

public class NativeMessagingTests
{
    [Fact]
    public async Task ReadMessage_Returns_Parsed_Json()
    {
        var payload = new { type = "event", value = 42 };
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var frame = new byte[4 + json.Length];
        BitConverter.GetBytes(json.Length).CopyTo(frame, 0);
        json.CopyTo(frame, 4);

        using var stream = new MemoryStream(frame);
        var doc = await NativeMessaging.ReadMessageAsync(stream);

        Assert.NotNull(doc);
        Assert.Equal("event", doc!.RootElement.GetProperty("type").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task ReadMessage_Returns_Null_On_Eof()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        var doc = await NativeMessaging.ReadMessageAsync(stream);
        Assert.Null(doc);
    }

    [Fact]
    public async Task ReadMessage_Throws_On_Oversized_Message()
    {
        var lengthBytes = BitConverter.GetBytes(NativeMessaging.MaxMessageSize + 1);
        using var stream = new MemoryStream(lengthBytes);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NativeMessaging.ReadMessageAsync(stream));
    }

    [Fact]
    public async Task WriteMessage_Produces_Valid_Frame()
    {
        var payload = new { type = "decision", value = "ok" };
        using var stream = new MemoryStream();
        await NativeMessaging.WriteMessageAsync(stream, payload);

        stream.Position = 0;
        var lengthBuf = new byte[4];
        await stream.ReadAsync(lengthBuf);
        int length = BitConverter.ToInt32(lengthBuf);

        var jsonBuf = new byte[length];
        await stream.ReadAsync(jsonBuf);
        var doc = JsonDocument.Parse(jsonBuf);

        Assert.Equal("decision", doc.RootElement.GetProperty("type").GetString());
    }
}
