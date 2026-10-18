using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Adapter.Milky.Net;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests the <see cref="MilkyAdapter" /> message recording contract for received and sent messages.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class MilkyMessageRecordingTests
{
    private const long SelfId = 12345;

#region Outbound

    /// <summary>
    ///     A successful <see cref="MilkyBotApi.SendGroupMessageAsync" /> records only the submitted segments with the
    ///     protocol message ID and <see cref="SendMessageResult.Time" />.
    /// </summary>
    [Fact]
    public async Task SendGroupMessage_RecordsSubmittedSegmentsAndProtocolTime()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        (MilkyAdapter adapter, CaptureSink sink) = CreateAdapter(server);
        using MilkyHttpApiClient client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi api = new(client, adapter);
        TextSegment text = new() { Text = "hi" };
        // A zero mention target is not convertible and is therefore not submitted.
        MessageBody body = new([text, new MentionSegment { Target = 0L }]);

        Task<SendMessageResult> call = api.SendGroupMessageAsync(654321, body, deadline.Token).AsTask();
        JObject request = await RespondAsync(server, OkResponse(77, 1700000000), deadline.Token);
        SendMessageResult result = await call.WaitAsync(deadline.Token);

        DateTime protocolTime = DateTimeOffset.FromUnixTimeSeconds(1700000000).LocalDateTime;
        Assert.True(result.IsSuccess);
        Assert.Equal(protocolTime, result.Time);
        Assert.Single((JArray)request["message"]!);
        MessageRecord record = Assert.Single(sink.Records);
        Assert.Equal(RecordDirection.Outbound, record.Direction);
        Assert.Equal("Milky", record.ProtocolName);
        Assert.Equal(MessageSourceType.Group, record.SourceType);
        Assert.Equal(654321L, (long)record.GroupId);
        Assert.Equal(77L, (long)record.MessageId);
        Assert.Equal(protocolTime, record.Time);
        TextSegment recorded = Assert.IsType<TextSegment>(Assert.Single(record.Segments));
        Assert.Equal("hi", recorded.Text);
        Assert.NotSame(text, recorded);
    }

    /// <summary><see cref="ForwardRecordSegment" /> keeps only the nested segments that were submitted.</summary>
    [Fact]
    public async Task SendFriendForward_RecordsSubmittedNestedSegments()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        (MilkyAdapter adapter, CaptureSink sink) = CreateAdapter(server);
        using MilkyHttpApiClient client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi api = new(client, adapter);
        ForwardSegment forward = new()
        {
            Messages =
            [
                new ForwardedMessageNode
                {
                    UserId   = 222L, SenderName = "nested",
                    Segments = new MessageBody([new TextSegment { Text = "inner" }, new MentionSegment { Target = 0L }])
                }
            ]
        };

        Task<SendMessageResult> call = api.SendFriendMessageAsync(222, new MessageBody(forward), deadline.Token).AsTask();
        await RespondAsync(server, OkResponse(78, 1700000001), deadline.Token);
        Assert.True((await call.WaitAsync(deadline.Token)).IsSuccess);

        MessageRecord record = Assert.Single(sink.Records);
        Assert.Equal(MessageSourceType.Friend, record.SourceType);
        Assert.Equal(222L, (long)record.PeerUserId);
        Assert.Equal(SelfId, (long)record.SenderId);
        ForwardRecordSegment recorded = Assert.IsType<ForwardRecordSegment>(Assert.Single(record.Segments));
        MessageRecord        node     = Assert.Single(recorded.Messages);
        Assert.Equal("nested", node.SenderName);
        Assert.Equal("inner", Assert.IsType<TextSegment>(Assert.Single(node.Segments)).Text);
    }

    /// <summary>A failed <see cref="MilkyBotApi.SendFriendMessageAsync" /> produces no record.</summary>
    [Fact]
    public async Task SendFailure_ProducesNoRecord()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        (MilkyAdapter adapter, CaptureSink sink) = CreateAdapter(server);
        using MilkyHttpApiClient client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        MilkyBotApi api = new(client, adapter);

        Task<SendMessageResult> call = api.SendFriendMessageAsync(222, "hi", deadline.Token).AsTask();
        JObject failed = new() { ["status"] = "failed", ["retcode"] = 123, ["message"] = "rejected", ["data"] = null };
        await RespondAsync(server, failed, deadline.Token);

        Assert.False((await call.WaitAsync(deadline.Token)).IsSuccess);
        Assert.Empty(sink.Records);
    }

#endregion

#region Inbound

    /// <summary><see cref="MilkyAdapter.HandleEventMessage" /> records a received message before dispatching its event.</summary>
    [Fact]
    public void ReceivedMessage_IsRecordedBeforeDispatch()
    {
        (MilkyAdapter adapter, CaptureSink sink) = CreateAdapter(null);
        int recordsAtDispatch = -1;
        ((IAdapterEventSource)adapter).OnEvent += _ =>
        {
            recordsAtDispatch = sink.Records.Count;
            return ValueTask.CompletedTask;
        };

        adapter.HandleEventMessage(MessageEventJson(senderId: 222));

        Assert.Equal(1, recordsAtDispatch);
        MessageRecord record = Assert.Single(sink.Records);
        Assert.Equal(RecordDirection.Inbound, record.Direction);
        Assert.Equal(222L, (long)record.SenderId);
        Assert.Equal(999L, (long)record.MessageId);
        Assert.Equal("hello", Assert.IsType<TextSegment>(Assert.Single(record.Segments)).Text);
        Assert.Equal("TestGroup", record.Group?.GroupName);
    }

    /// <summary>
    ///     Messages from the bot account are never recorded as inbound, even when
    ///     <see cref="MilkyConfig.DropSelfMessage" /> is false and they are dispatched.
    /// </summary>
    [Fact]
    public void SelfMessage_IsNotRecordedEvenWhenDispatched()
    {
        (MilkyAdapter adapter, CaptureSink sink) = CreateAdapter(null, dropSelfMessage: false);
        int dispatched = 0;
        ((IAdapterEventSource)adapter).OnEvent += _ =>
        {
            dispatched++;
            return ValueTask.CompletedTask;
        };

        adapter.HandleEventMessage(MessageEventJson(senderId: SelfId));

        Assert.Equal(1, dispatched);
        Assert.Empty(sink.Records);
    }

#endregion

#region Helpers

    private static (MilkyAdapter Adapter, CaptureSink Sink) CreateAdapter(
        TcpListener? server,
        bool         dropSelfMessage = true)
    {
        MilkyAdapter adapter = new(
            new MilkyConfig
            {
                Port            = server is null ? 3010 : ((IPEndPoint)server.LocalEndpoint).Port,
                DropSelfMessage = dropSelfMessage
            });
        CaptureSink sink = new();
        ((IBotAdapter)adapter).AttachMessageRecordSink(sink);
        typeof(MilkyAdapter).GetProperty(nameof(MilkyAdapter.SelfId))!.SetValue(adapter, (UserId)SelfId);
        return (adapter, sink);
    }

    private static string MessageEventJson(long senderId) =>
        new JObject
        {
            ["time"] = 1700000000, ["self_id"] = SelfId, ["event_type"] = "message_receive",
            ["data"] = JObject.Parse(
                $$$"""
                  {
                      "message_scene": "group", "peer_id": 111, "message_seq": 999,
                      "sender_id": {{{senderId}}}, "time": 1700000000,
                      "segments": [{"type": "text", "data": {"text": "hello"}}],
                      "group": {"group_id": 111, "group_name": "TestGroup", "member_count": 10, "max_member_count": 200},
                      "group_member": {"user_id": {{{senderId}}}, "nickname": "User", "sex": "male", "group_id": 111, "card": "", "title": "", "level": 1, "role": "member", "join_time": 0, "last_sent_time": 0}
                  }
                  """)
        }.ToString(Formatting.None);

    private static JObject OkResponse(long messageSeq, long time) => new()
    {
        ["status"] = "ok", ["retcode"] = 0,
        ["data"]   = new JObject { ["message_seq"] = messageSeq, ["time"] = time }
    };

    /// <summary>Captures one ASCII JSON request and replies with the supplied protocol envelope.</summary>
    private static async Task<JObject> RespondAsync(TcpListener server, JObject responseBody, CancellationToken ct)
    {
        using TcpClient    peer          = await server.AcceptTcpClientAsync(ct);
        using StreamReader reader        = new(peer.GetStream(), leaveOpen: true);
        _ = await reader.ReadLineAsync(ct);
        int contentLength = 0;
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
        return JObject.Parse(new string(requestBody));
    }

    /// <summary>Sink that captures records synchronously.</summary>
    private sealed class CaptureSink : IMessageRecordSink
    {
        private readonly ConcurrentQueue<MessageRecord> _records = new();

        public List<MessageRecord> Records => [.. _records];

        public bool IsEnabled => true;

        public void Write(MessageRecord record) => _records.Enqueue(record);
    }

#endregion
}
