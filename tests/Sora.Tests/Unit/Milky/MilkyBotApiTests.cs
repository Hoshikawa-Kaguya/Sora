using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Adapter.Milky.Net;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests the Milky API requests and public results through HTTP.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class MilkyBotApiTests
{
#region Message Sending

    /// <summary>
    ///     Re-sending a received body converts it at the send entry: incoming-only segments are dropped, a received
    ///     image is sent through its URL, and the received content is left unchanged.
    /// </summary>
    [Fact]
    public async Task SendGroupMessage_ReceivedBody_ConvertsBeforeSending()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        using MilkyHttpApiClient      client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi                   api = new(client, new MilkyAdapter(new MilkyConfig()));
        MessageContext received = new()
        {
            Body = MessageBody.FromIncoming(
            [
                new TextSegment { Text = "hi" },
                new FileSegment { FileId = "fid" },
                new ImageSegment { Url = "http://img.png" }
            ])
        };

        Task<SendMessageResult> call = api.SendGroupMessageAsync(654321, received.Body, deadline.Token).AsTask();
        JObject response = new()
            { ["status"] = "ok", ["retcode"] = 0, ["data"] = new JObject { ["message_seq"] = 1, ["time"] = 0 } };
        (string requestLine, JObject request) = await RespondAsync(server, response, deadline.Token);

        Assert.Equal("POST /api/send_group_message HTTP/1.1", requestLine);
        JArray message = Assert.IsType<JArray>(request["message"]);
        Assert.Equal(["text", "image"], message.Select(s => (string?)s["type"]));
        Assert.Equal("http://img.png", (string?)message[1]["data"]?["uri"]);
        Assert.True((await call.WaitAsync(deadline.Token)).IsSuccess);
        Assert.Equal("", Assert.IsType<ImageSegment>(received.Body[2]).FileUri);
    }

    /// <summary>A body that breaks the composition rules is rejected without a request.</summary>
    [Fact]
    public async Task SendFriendMessage_CompositionViolation_ReturnsInvalidMessage()
    {
        using MilkyHttpApiClient client = new(new MilkyConfig { Port = 1 });
        MilkyBotApi              api    = new(client, new MilkyAdapter(new MilkyConfig()));

        SendMessageResult result = await api.SendFriendMessageAsync(
            222,
            new MessageBody([new TextSegment { Text = "hi" }, new AudioSegment { FileUri = "base64://audio" }]),
            TestContext.Current.CancellationToken);

        Assert.Equal(ApiStatusCode.InvalidMessage, result.Code);
        Assert.Contains("Audio", result.ErrorMessage);
    }

    /// <summary>A body with only unsendable segments is rejected as empty without a request.</summary>
    [Fact]
    public async Task SendFriendMessage_OnlyUnsendableSegments_ReturnsInvalidMessage()
    {
        using MilkyHttpApiClient client = new(new MilkyConfig { Port = 1 });
        MilkyBotApi              api    = new(client, new MilkyAdapter(new MilkyConfig()));

        SendMessageResult result = await api.SendFriendMessageAsync(
            222,
            MessageBody.FromIncoming([new FileSegment { FileId = "fid" }]),
            TestContext.Current.CancellationToken);

        Assert.Equal(ApiStatusCode.InvalidMessage, result.Code);
        Assert.Equal("Empty message", result.ErrorMessage);
    }

#endregion

    /// <summary>Persists the requested file and preserves protocol success or failure.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(123)]
    public async Task PersistGroupFile_SendsIdentifiersAndReturnsResult(int retCode)
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        using MilkyHttpApiClient      client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi                   api = new(client, new MilkyAdapter(new MilkyConfig()));
        Task<ApiResult>               call = api.PersistGroupFileAsync(123456, "test-file", deadline.Token).AsTask();

        string message = retCode == 0 ? "" : "file is unavailable";
        JObject response = new()
        {
            ["status"] = retCode == 0 ? "ok" : "failed", ["retcode"] = retCode, ["message"] = message,
            ["data"]   = new JObject()
        };
        (string requestLine, JObject request) = await RespondAsync(server, response, deadline.Token);

        Assert.Equal("POST /api/persist_group_file HTTP/1.1", requestLine);
        Assert.Equal(123456, (long?)request["group_id"]);
        Assert.Equal("test-file", (string?)request["file_id"]);
        ApiResult result = await call.WaitAsync(deadline.Token);
        Assert.Equal(retCode == 0, result.IsSuccess);
        Assert.Equal(retCode == 0 ? ApiStatusCode.Ok : ApiStatusCode.Unknown, result.Code);
        if (retCode != 0) Assert.Equal(message, result.Message);
    }

    /// <summary>Defaults to received files and explicitly selects files sent by this bot.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPrivateFileDownloadUrl_SendsDirectionAndReturnsUrl(bool isSelfSend)
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        using MilkyHttpApiClient      client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi                   api = new(client, new MilkyAdapter(new MilkyConfig()));
        Task<ApiResult<string>> call = (isSelfSend
            ? api.GetPrivateFileDownloadUrlAsync(654321, "test-file", "test-hash", true, deadline.Token)
            : api.GetPrivateFileDownloadUrlAsync(654321, "test-file", "test-hash", ct: deadline.Token)).AsTask();

        const string url = "https://example.test/file";
        JObject response = new()
            { ["status"] = "ok", ["retcode"] = 0, ["data"] = new JObject { ["download_url"] = url } };
        (string requestLine, JObject request) = await RespondAsync(server, response, deadline.Token);

        Assert.Equal("POST /api/get_private_file_download_url HTTP/1.1", requestLine);
        Assert.Equal(654321, (long?)request["user_id"]);
        Assert.Equal("test-file", (string?)request["file_id"]);
        Assert.Equal("test-hash", (string?)request["file_hash"]);
        Assert.Equal(isSelfSend, (bool?)request["is_self_send"]);
        ApiResult<string> result = await call.WaitAsync(deadline.Token);
        Assert.Equal(url, result.AssertSuccess());
    }

    /// <summary>Captures one ASCII JSON request and replies with the supplied protocol envelope.</summary>
    private static async Task<(string RequestLine, JObject Request)> RespondAsync(
        TcpListener       server,
        JObject           responseBody,
        CancellationToken ct)
    {
        using TcpClient    peer          = await server.AcceptTcpClientAsync(ct);
        using StreamReader reader        = new(peer.GetStream(), leaveOpen: true);
        string             requestLine   = await reader.ReadLineAsync(ct) ?? "";
        int                contentLength = 0;
        while (await reader.ReadLineAsync(ct) is { Length: > 0 } header)
            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                contentLength = int.Parse(header["Content-Length:".Length..], CultureInfo.InvariantCulture);

        char[] requestBody    = new char[contentLength];
        int    charactersRead = await reader.ReadBlockAsync(requestBody, ct);
        Assert.Equal(contentLength, charactersRead);
        string body = responseBody.ToString(Formatting.None);
        string response =
            $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}";
        await peer.GetStream().WriteAsync(Encoding.UTF8.GetBytes(response), ct);
        return (requestLine, JObject.Parse(new string(requestBody)));
    }
}