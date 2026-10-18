using System.Collections.Concurrent;
using Sora.Entities.MessageWaiting;
using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>
///     Tests <see cref="MessageRecord" />, <see cref="SoraService.UseMessageRecorder" /> and the per-recorder queue
///     delivery of the service-owned record sink.
/// </summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class MessageRecorderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static CancellationToken TestCt => TestContext.Current.CancellationToken;

#region Record Model

    /// <summary>
    ///     <see cref="MessageRecord.Inbound" /> keeps the received content when the event segments are later
    ///     converted for sending.
    /// </summary>
    [Fact]
    public void Inbound_SegmentsAreIndependentOfEvent()
    {
        ImageSegment image = new() { Url = "https://example.test/a.png", Direction = SegmentDirection.Incoming };
        MessageReceivedEvent message = Message(
            1,
            200L,
            MessageBody.FromIncoming([new TextSegment { Text = "hi" }, image]));

        MessageRecord record = MessageRecord.Inbound("Mock", message);
        // Re-sending the received body converts its segments in place.
        _ = new MessageBody(message.Message.Body);

        ImageSegment recorded = Assert.IsType<ImageSegment>(record.Segments[1]);
        Assert.NotSame(image, recorded);
        Assert.Equal(SegmentDirection.Outgoing, image.Direction);
        Assert.Equal(SegmentDirection.Incoming, recorded.Direction);
        Assert.Equal("", recorded.FileUri);
        Assert.Equal(RecordDirection.Inbound, record.Direction);
        Assert.Equal("Mock", record.ProtocolName);
        Assert.Equal(message.ConnectionId, record.ConnectionId);
        Assert.Equal(200L, (long)record.SenderId);
        Assert.Equal(1L, (long)record.MessageId);
        Assert.Equal(message.Message.Time, record.Time);
        Assert.Equal(default, record.PeerUserId);
        Assert.Same(message.Member, record.Member);
    }

    /// <summary><see cref="ForwardSegment" /> becomes a <see cref="ForwardRecordSegment" /> that keeps the nested content.</summary>
    [Fact]
    public void Forward_KeepsNestedContentAsRecords()
    {
        DateTime nodeTime = new(
            2026,
            10,
            18,
            12,
            0,
            0,
            DateTimeKind.Local);
        ForwardSegment forward = new()
        {
            Title = "title",
            Messages =
            [
                new ForwardedMessageNode
                {
                    UserId = 300L, SenderName = "nested", Time = nodeTime, Segments = new MessageBody("inner")
                }
            ]
        };

        MessageRecord record = MessageRecord.Outbound(
            "Mock",
            Guid.NewGuid(),
            1L,
            MessageSourceType.Group,
            default,
            100L,
            [forward]);

        ForwardRecordSegment recorded = Assert.IsType<ForwardRecordSegment>(Assert.Single(record.Segments));
        MessageRecord        node     = Assert.Single(recorded.Messages);
        Assert.Equal("title", recorded.Title);
        Assert.Equal(RecordDirection.Outbound, node.Direction);
        Assert.Equal(300L, (long)node.SenderId);
        Assert.Equal("nested", node.SenderName);
        Assert.Equal(nodeTime, node.Time);
        TextSegment text = Assert.IsType<TextSegment>(Assert.Single(node.Segments));
        Assert.Equal("inner", text.Text);
        Assert.NotSame(forward.Messages[0].Segments[0], text);
    }

    /// <summary>
    ///     <see cref="ForwardRecordSegment.Direction" /> is incoming for received forwards and outgoing, including
    ///     nested forwards, for forwards built by the user and sent.
    /// </summary>
    [Fact]
    public void Forward_DirectionFollowsRecordDirection()
    {
        ForwardSegment received = new() { ForwardId = "forward-id" };
        ForwardSegment nested = new()
        {
            Messages = [new ForwardedMessageNode { UserId = 300L, Segments = new MessageBody("deep") }]
        };
        ForwardSegment sent = new()
        {
            Messages = [new ForwardedMessageNode { UserId = 300L, Segments = new MessageBody(nested) }]
        };

        MessageRecord inbound = MessageRecord.Inbound("Mock", Message(1, 200L, MessageBody.FromIncoming([received])));
        MessageRecord outbound = MessageRecord.Outbound(
            "Mock",
            Guid.NewGuid(),
            1L,
            MessageSourceType.Group,
            default,
            100L,
            [sent]);

        ForwardRecordSegment inboundForward = Assert.IsType<ForwardRecordSegment>(Assert.Single(inbound.Segments));
        Assert.Equal(SegmentDirection.Incoming, inboundForward.Direction);
        Assert.Equal("forward-id", inboundForward.ForwardId);
        ForwardRecordSegment outboundForward = Assert.IsType<ForwardRecordSegment>(Assert.Single(outbound.Segments));
        Assert.Equal(SegmentDirection.Outgoing, outboundForward.Direction);
        ForwardRecordSegment nestedForward = Assert.IsType<ForwardRecordSegment>(
            Assert.Single(Assert.Single(outboundForward.Messages).Segments));
        Assert.Equal(SegmentDirection.Outgoing, nestedForward.Direction);
        Assert.Null(outboundForward.ToOutgoing());
    }

    /// <summary><see cref="ForwardRecordSegment.ToOutgoing" /> returns null, so a record segment cannot be sent.</summary>
    [Fact]
    public void ForwardRecordSegment_IsRejectedBySendableBody()
    {
        ForwardRecordSegment segment = new();
        MessageBody          body    = [];

        body.Add(segment);

        Assert.Null(segment.ToOutgoing());
        Assert.Empty(body);
    }

    /// <summary><see cref="MessageRecord.Accept" /> uses the protocol time and falls back to the local response time.</summary>
    [Fact]
    public void Outbound_AcceptUsesProtocolTimeOrLocalFallback()
    {
        DateTime protocolTime = new(
            2026,
            10,
            18,
            8,
            0,
            0,
            DateTimeKind.Local);
        MessageRecord pending = MessageRecord.Outbound(
            "Mock",
            Guid.NewGuid(),
            1L,
            MessageSourceType.Friend,
            300L,
            default,
            [new TextSegment { Text = "hi" }]);

        MessageRecord withTime = pending.Accept(SendMessageResult.Ok(42L, protocolTime));
        DateTime      before   = DateTime.Now;
        MessageRecord fallback = pending.Accept(SendMessageResult.Ok(43L));

        Assert.Equal(42L, (long)withTime.MessageId);
        Assert.Equal(protocolTime, withTime.Time);
        Assert.InRange(fallback.Time, before, DateTime.Now);
        Assert.Equal(1L, (long)withTime.SenderId);
        Assert.Equal(300L, (long)withTime.PeerUserId);
        Assert.Null(withTime.Sender);
    }

#endregion

#region Registration

    /// <summary>The sink attached through <see cref="IBotAdapter" /> is enabled only while a started service has recorders.</summary>
    [Fact]
    public async Task Sink_EnabledOnlyWhileRunningWithRecorders()
    {
        EventPipelineFilterTests.MockAdapter withoutRecorders = new();
        await using SoraService              idle             = new(withoutRecorders, new RecorderConfig());
        await idle.StartAsync(TestCt);
        Assert.False(withoutRecorders.RecordSink.IsEnabled);

        EventPipelineFilterTests.MockAdapter adapter = new();
        await using SoraService              service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(new CaptureRecorder());
        Assert.False(adapter.RecordSink.IsEnabled);
        await service.StartAsync(TestCt);
        Assert.True(adapter.RecordSink.IsEnabled);
        await service.StopAsync(TestCt);
        Assert.False(adapter.RecordSink.IsEnabled);
    }

    /// <summary><see cref="SoraService.UseMessageRecorder" /> requires registration before start with valid options.</summary>
    [Fact]
    public async Task Register_RejectsLateRegistrationAndInvalidOptions()
    {
        await using SoraService service = new(new EventPipelineFilterTests.MockAdapter(), new RecorderConfig());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
                                                       service.UseMessageRecorder(
                                                           new CaptureRecorder(),
                                                           new MessageRecorderOptions { Capacity = 0 }));
        await service.StartAsync(TestCt);
        Assert.Throws<InvalidOperationException>(() => service.UseMessageRecorder(new CaptureRecorder()));
    }

#endregion

#region Pipeline Position

    /// <summary>Messages from <see cref="IBotServiceConfig.BlockUsers" /> are recorded even though the service drops them.</summary>
    [Fact]
    public async Task BlockedUserMessage_IsRecorded()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        CaptureRecorder                      recorder = new();
        await using SoraService              service  = new(adapter, new RecorderConfig { BlockUsers = [200L] });
        service.UseMessageRecorder(recorder);
        service.Events.OnMessageReceived += _ => throw new InvalidOperationException("Blocked event was dispatched");
        await service.StartAsync(TestCt);

        MessageReceivedEvent blocked = Message(1, 200L);
        await adapter.RaiseEventAsync(blocked);
        await service.StopAsync(TestCt);

        Assert.False(blocked.IsContinueEventChain);
        Assert.Equal([1L], recorder.MessageIds);
    }

    /// <summary>A message consumed by a <see cref="MessageWaiter" /> is still recorded.</summary>
    [Fact]
    public async Task WaiterConsumedMessage_IsRecorded()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        CaptureRecorder                      recorder = new();
        await using SoraService              service  = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        TaskCompletionSource<Task<MessageReceivedEvent?>> waiterRegistered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Events.OnMessageReceived += e =>
        {
            if ((long)e.Message.MessageId == 1)
                waiterRegistered.TrySetResult(e.WaitForNextMessageAsync(Timeout, TestCt).AsTask());
            return ValueTask.CompletedTask;
        };
        await service.StartAsync(TestCt);
        Guid connectionId = Guid.NewGuid();

        await adapter.RaiseEventAsync(Message(1, 200L, connectionId: connectionId));
        Task<MessageReceivedEvent?> waiter = await waiterRegistered.Task.WaitAsync(Timeout, TestCt);
        MessageReceivedEvent        reply  = Message(2, 200L, connectionId: connectionId);
        await adapter.RaiseEventAsync(reply);

        Assert.Same(reply, await waiter.WaitAsync(Timeout, TestCt));
        Assert.Null(reply.PipelineContext);
        await service.StopAsync(TestCt);
        Assert.Equal([1L, 2L], recorder.MessageIds);
    }

    /// <summary>The adapter contract (<c>WriteInbound</c>) never records messages sent by the bot account.</summary>
    [Fact]
    public async Task SelfMessage_IsNotRecorded()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        CaptureRecorder                      recorder = new();
        await using SoraService              service  = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);

        await adapter.RaiseEventAsync(Message(1, adapter.SelfId));
        await adapter.RaiseEventAsync(Message(2, 200L));
        await service.StopAsync(TestCt);

        Assert.Equal([2L], recorder.MessageIds);
    }

    /// <summary>The record is queued before any <see cref="IEventPreFilter" /> runs.</summary>
    [Fact]
    public async Task Record_IsQueuedBeforePreFilters()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        TaskCompletionSource                 recorded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaptureRecorder recorder = new((_, _) =>
        {
            recorded.TrySetResult();
            return ValueTask.CompletedTask;
        });
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        bool recordedBeforeFilterFinished = false;
        service.UseEventPreFilter(
            new AsyncFilter(async () =>
            {
                // Only a record queued before this filter can complete while the filter waits.
                await recorded.Task.WaitAsync(Timeout, TestCt);
                recordedBeforeFilterFinished = true;
            }));
        await service.StartAsync(TestCt);

        await adapter.RaiseEventAsync(Message(1, 200L));

        Assert.True(recordedBeforeFilterFinished);
    }

#endregion

#region Delivery

    /// <summary>One <see cref="IMessageRecorder" /> receives records in queue order and is never called concurrently.</summary>
    [Fact]
    public async Task Recorder_ReceivesRecordsSequentiallyInOrder()
    {
        EventPipelineFilterTests.MockAdapter adapter   = new();
        int                                  active    = 0;
        int                                  maxActive = 0;
        CaptureRecorder recorder = new(async (_, _) =>
        {
            int current = Interlocked.Increment(ref active);
            InterlockedMax(ref maxActive, current);
            await Task.Yield();
            Interlocked.Decrement(ref active);
        });
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);

        for (long id = 1; id <= 20; id++)
            await adapter.RaiseEventAsync(Message(id, 200L));
        await service.StopAsync(TestCt);

        Assert.Equal([.. Enumerable.Range(1, 20).Select(i => (long)i)], recorder.MessageIds);
        Assert.Equal(1, maxActive);
    }

    /// <summary>A blocked <see cref="IMessageRecorder" /> never delays event processing; overflowing records are dropped.</summary>
    [Fact]
    public async Task BlockedRecorder_DoesNotBlockProcessing()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        using ManualResetEventSlim           entered = new();
        using ManualResetEventSlim           release = new();
        CaptureRecorder recorder = new((record, _) =>
        {
            if ((long)record.MessageId == 1)
            {
                // ReSharper disable once AccessToDisposedClosure
                entered.Set();
                // Synchronous blocking must stay on the consumer.
                // ReSharper disable once AccessToDisposedClosure
                release.Wait(Timeout, TestCt);
            }

            return ValueTask.CompletedTask;
        });
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder, new MessageRecorderOptions { Capacity = 1 });
        int dispatched = 0;
        service.Events.OnMessageReceived += _ =>
        {
            Interlocked.Increment(ref dispatched);
            return ValueTask.CompletedTask;
        };
        await service.StartAsync(TestCt);

        await adapter.RaiseEventAsync(Message(1, 200L)).AsTask().WaitAsync(Timeout, TestCt);
        Assert.True(entered.Wait(Timeout, TestCt));
        await adapter.RaiseEventAsync(Message(2, 200L)).AsTask().WaitAsync(Timeout, TestCt);
        await adapter.RaiseEventAsync(Message(3, 200L)).AsTask().WaitAsync(Timeout, TestCt);
        Assert.Equal(3, dispatched);

        release.Set();
        await service.StopAsync(TestCt);
        Assert.Equal([1L, 2L], recorder.MessageIds);
    }

    /// <summary>
    ///     A full <see cref="MessageRecorderOptions.Capacity" /> queue drops only that recorder's records; other
    ///     recorders still receive every record.
    /// </summary>
    [Fact]
    public async Task FullQueue_DoesNotAffectOtherRecorders()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        TaskCompletionSource                 entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource                 release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaptureRecorder blocked = new(async (record, _) =>
        {
            if ((long)record.MessageId != 1) return;
            entered.TrySetResult();
            await release.Task.WaitAsync(Timeout, TestCt);
        });
        CaptureRecorder         other   = new();
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(blocked, new MessageRecorderOptions { Capacity = 1 });
        service.UseMessageRecorder(other);
        await service.StartAsync(TestCt);

        await adapter.RaiseEventAsync(Message(1, 200L)).AsTask().WaitAsync(Timeout, TestCt);
        await entered.Task.WaitAsync(Timeout, TestCt);
        for (long id = 2; id <= 4; id++)
            await adapter.RaiseEventAsync(Message(id, 200L)).AsTask().WaitAsync(Timeout, TestCt);

        release.SetResult();
        await service.StopAsync(TestCt);
        Assert.Equal([1L, 2L], blocked.MessageIds);
        Assert.Equal([1L, 2L, 3L, 4L], other.MessageIds);
    }

    /// <summary>An <see cref="IMessageRecorder.OnMessageAsync" /> failure is isolated and later records are still delivered.</summary>
    [Fact]
    public async Task RecorderException_ContinuesWithNextRecord()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        CaptureRecorder recorder = new((record, _) => (long)record.MessageId == 1
                                           ? throw new InvalidOperationException("recorder failure")
                                           : ValueTask.CompletedTask);
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);

        await adapter.RaiseEventAsync(Message(1, 200L));
        await adapter.RaiseEventAsync(Message(2, 200L));
        await service.StopAsync(TestCt);

        Assert.Equal([1L, 2L], recorder.MessageIds);
    }

#endregion

#region Lifecycle

    /// <summary><see cref="SoraService.StopAsync" /> drains every queued record.</summary>
    [Fact]
    public async Task Stop_DrainsQueuedRecords()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        CaptureRecorder                      recorder = new(async (_, ct) => await Task.Delay(5, ct));
        await using SoraService              service  = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);

        for (long id = 1; id <= 10; id++)
            await adapter.RaiseEventAsync(Message(id, 200L));
        await service.StopAsync(TestCt).AsTask().WaitAsync(Timeout, TestCt);

        Assert.Equal(10, recorder.MessageIds.Count);
    }

    /// <summary>A canceled <see cref="SoraService.StopAsync" /> drain returns, cancels the recorder token and drops the rest.</summary>
    [Fact]
    public async Task Stop_CanceledDrainCancelsRecorder()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        TaskCompletionSource                 entered  = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource                 canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CaptureRecorder recorder = new(async (_, ct) =>
        {
            entered.TrySetResult();
            await using CancellationTokenRegistration registration = ct.Register(() => canceled.TrySetResult());
            await Task.Delay(System.Threading.Timeout.Infinite, ct);
        });
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);
        await adapter.RaiseEventAsync(Message(1, 200L));
        await adapter.RaiseEventAsync(Message(2, 200L));
        await entered.Task.WaitAsync(Timeout, TestCt);

        using CancellationTokenSource stopDeadline = new(TimeSpan.FromMilliseconds(100));
        await service.StopAsync(stopDeadline.Token).AsTask().WaitAsync(Timeout, TestCt);

        await canceled.Task.WaitAsync(Timeout, TestCt);
        Assert.Equal([1L], recorder.MessageIds);
    }

    /// <summary>
    ///     After an abandoned drain, <see cref="SoraService.StartAsync" /> waits for the recorder call that ignored
    ///     cancellation, so the recorder is never called concurrently.
    /// </summary>
    [Fact]
    public async Task Restart_WaitsForAbandonedRecorderCall()
    {
        EventPipelineFilterTests.MockAdapter adapter   = new();
        TaskCompletionSource                 entered   = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource                 release   = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int                                  active    = 0;
        int                                  maxActive = 0;
        CaptureRecorder recorder = new(async (record, _) =>
        {
            int current = Interlocked.Increment(ref active);
            InterlockedMax(ref maxActive, current);
            if ((long)record.MessageId == 1)
            {
                entered.TrySetResult();
                // Deliberately ignores the recorder token.
                await release.Task.WaitAsync(Timeout, TestCt);
            }

            Interlocked.Decrement(ref active);
        });
        await using SoraService service = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);
        await adapter.RaiseEventAsync(Message(1, 200L));
        await entered.Task.WaitAsync(Timeout, TestCt);
        using CancellationTokenSource stopDeadline = new(TimeSpan.FromMilliseconds(50));
        await service.StopAsync(stopDeadline.Token).AsTask().WaitAsync(Timeout, TestCt);

        Task restart = service.StartAsync(TestCt).AsTask();
        await Task.Delay(100, TestCt);
        Assert.False(restart.IsCompleted);
        Assert.False(adapter.RecordSink.IsEnabled);

        release.SetResult();
        await restart.WaitAsync(Timeout, TestCt);
        await adapter.RaiseEventAsync(Message(2, 200L));
        await service.StopAsync(TestCt);

        Assert.Equal([1L, 2L], recorder.MessageIds);
        Assert.Equal(1, maxActive);
    }

    /// <summary>When the adapter fails to stop, <see cref="SoraService.StopAsync" /> still drains and rethrows.</summary>
    [Fact]
    public async Task AdapterStopFailure_StillDrainsQueue()
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        CaptureRecorder                      recorder = new(async (_, ct) => await Task.Delay(5, ct));
        await using SoraService              service  = new(adapter, new RecorderConfig());
        service.UseMessageRecorder(recorder);
        await service.StartAsync(TestCt);
        for (long id = 1; id <= 3; id++)
            await adapter.RaiseEventAsync(Message(id, 200L));

        adapter.StopException = new InvalidOperationException("adapter stop failed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StopAsync(TestCt).AsTask());
        adapter.StopException = null;

        Assert.False(adapter.RecordSink.IsEnabled);
        Assert.Equal([1L, 2L, 3L], recorder.MessageIds);
    }

#endregion

#region Helpers

    private static MessageReceivedEvent Message(
        long         messageId,
        long         senderId,
        MessageBody? body         = null,
        Guid?        connectionId = null) => new()
    {
        Api          = null!,
        ConnectionId = connectionId ?? Guid.NewGuid(),
        SelfId       = 1L,
        Message = new MessageContext
        {
            MessageId  = messageId,
            SourceType = MessageSourceType.Group,
            GroupId    = 100L,
            SenderId   = senderId,
            Time = new DateTime(
                2026,
                10,
                18,
                9,
                0,
                0,
                DateTimeKind.Local),
            Body = body ?? new MessageBody("record")
        },
        Member = new GroupMemberInfo { UserId = senderId, GroupId = 100L }
    };

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current) return;
            current = observed;
        }
    }

    /// <summary>Recorder that captures message IDs in delivery order and runs an optional callback.</summary>
    private sealed class CaptureRecorder : IMessageRecorder
    {
        private readonly Func<MessageRecord, CancellationToken, ValueTask>? _callback;
        private readonly ConcurrentQueue<long>                              _messageIds = new();

        public CaptureRecorder(Func<MessageRecord, CancellationToken, ValueTask>? callback = null)
        {
            _callback = callback;
        }

        public List<long> MessageIds => [.. _messageIds];

        public async ValueTask OnMessageAsync(MessageRecord record, CancellationToken ct)
        {
            _messageIds.Enqueue(record.MessageId);
            if (_callback is not null)
                await _callback(record, ct);
        }
    }

    private sealed class AsyncFilter : IEventPreFilter
    {
        private readonly Func<Task> _callback;

        public AsyncFilter(Func<Task> callback)
        {
            _callback = callback;
        }

        public async ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            await _callback();
            return true;
        }
    }

    private sealed class RecorderConfig : IBotServiceConfig
    {
        public UserId[] BlockUsers           { get; init; } = [];
        public UserId[] SuperUsers           => [];
        public bool     EnableCommandManager => false;
        public bool     AutoMarkMessageRead  => false;
    }

#endregion
}