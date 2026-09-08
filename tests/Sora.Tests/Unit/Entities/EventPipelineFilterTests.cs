using Xunit;
using Sora.Entities.MessageWaiting;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for the event pipeline filter integration in <see cref="Sora.SoraService" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class EventPipelineFilterTests : IAsyncDisposable
{
    private readonly MockAdapter _adapter = new();
    private readonly SoraService _service;

    public EventPipelineFilterTests()
    {
        _service = new SoraService(_adapter, new MockServiceConfig());
    }

    public async ValueTask DisposeAsync() => await _service.DisposeAsync();

    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region PreFilter Tests

    /// <see cref="IEventPreFilter.OnEventAsync" />
    [Fact]
    public async Task PreFilter_ReturnsTrue_EventReachesDispatcher()
    {
        bool dispatched = false;
        _service.Events.OnMessageReceived += async _ =>
        {
            dispatched = true;
            await ValueTask.CompletedTask;
        };
        _service.UseEventPreFilter(new AlwaysPassPreFilter());

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.True(dispatched);
    }

    /// <see cref="IEventPreFilter.OnEventAsync" />
    [Fact]
    public async Task PreFilter_ReturnsFalse_EventBlockedFromDispatcher()
    {
        bool dispatched = false;
        _service.Events.OnMessageReceived += async _ =>
        {
            dispatched = true;
            await ValueTask.CompletedTask;
        };
        _service.UseEventPreFilter(new AlwaysBlockPreFilter());

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.False(dispatched);
    }

    /// <see cref="IEventPreFilter.Order" />
    [Fact]
    public async Task PreFilters_ExecuteInOrderAscending()
    {
        List<int> order = [];
        _service.UseEventPreFilter(new OrderedPreFilter(30, order));
        _service.UseEventPreFilter(new OrderedPreFilter(10, order));
        _service.UseEventPreFilter(new OrderedPreFilter(20, order));

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal([10, 20, 30], order);
    }

    /// <see cref="IEventPreFilter.OnEventAsync" />
    [Fact]
    public async Task PreFilter_ThrowsException_TreatedAsPassThrough()
    {
        bool dispatched = false;
        _service.Events.OnMessageReceived += async _ =>
        {
            dispatched = true;
            await ValueTask.CompletedTask;
        };
        _service.UseEventPreFilter(new ThrowingPreFilter());

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.True(dispatched);
    }

    /// <summary>Cancellation from a pre-filter propagates to the adapter event invocation.</summary>
    [Fact]
    public async Task PreFilter_ThrowsCancellation_Propagates()
    {
        using CancellationTokenSource source = new();
        CancellingPreFilter cancellingFilter = new() { Cancel = source.Cancel };
        RecordingPreFilter  secondFilter     = new();
        RecordingPostFilter postFilter = new();
        _service.UseEventPreFilter(cancellingFilter);
        _service.UseEventPreFilter(secondFilter);
        _service.UseEventPostFilter(postFilter);
        await _service.StartAsync(source.Token);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                                                                 await _adapter.RaiseEventAsync(CreateMessageEvent()));

        Assert.Same(cancellingFilter.Exception, exception);
        Assert.Equal(1, cancellingFilter.CallCount);
        Assert.Equal(0, secondFilter.CallCount);
        Assert.Equal(0, postFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.OnEventAsync" />
    [Fact]
    public async Task PreFilter_SecondFilterBlocks_FirstAlreadyExecuted()
    {
        List<int> order = [];
        _service.UseEventPreFilter(new OrderedPreFilter(1, order));
        _service.UseEventPreFilter(new BlockingOrderedPreFilter(2, order));
        _service.UseEventPreFilter(new OrderedPreFilter(3, order));

        bool dispatched = false;
        _service.Events.OnMessageReceived += async _ =>
        {
            dispatched = true;
            await ValueTask.CompletedTask;
        };

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal([1, 2], order); // Third filter not reached
        Assert.False(dispatched);
    }

    /// <see cref="PipelineContext" />
    [Fact]
    public async Task PreFilter_CanWriteToPipelineContext()
    {
        ContextWritingPreFilter preFilter = new();
        _service.UseEventPreFilter(preFilter);

        string? readValue = null;
        _service.Events.OnMessageReceived += async e =>
        {
            if (e.PipelineContext is not null && e.PipelineContext.TryGet("pre_key", out string val))
                readValue = val;
            await ValueTask.CompletedTask;
        };

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal("pre_value", readValue);
    }

#endregion

#region PostFilter Tests

    /// <see cref="IEventPostFilter.OnEventProcessedAsync" />
    [Fact]
    public async Task PostFilter_ExecutesWithChainCompletedTrue_WhenDispatched()
    {
        RecordingPostFilter postFilter = new();
        _service.UseEventPostFilter(postFilter);
        _service.Events.OnMessageReceived += async _ => await ValueTask.CompletedTask;

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(1, postFilter.CallCount);
        Assert.True(postFilter.LastChainCompleted);
    }

    /// <see cref="IEventPostFilter.OnEventProcessedAsync" />
    [Fact]
    public async Task PostFilter_ExecutesWithChainCompletedFalse_WhenPreFilterBlocks()
    {
        _service.UseEventPreFilter(new AlwaysBlockPreFilter());
        RecordingPostFilter postFilter = new();
        _service.UseEventPostFilter(postFilter);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(1, postFilter.CallCount);
        Assert.False(postFilter.LastChainCompleted);
    }

    /// <see cref="IEventPostFilter.OnEventProcessedAsync" />
    [Fact]
    public async Task PostFilter_ThrowsException_DoesNotCrashPipeline()
    {
        _service.UseEventPostFilter(new ThrowingPostFilter());
        RecordingPostFilter secondPost = new();
        _service.UseEventPostFilter(secondPost);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        // Second filter still executes despite first throwing
        Assert.Equal(1, secondPost.CallCount);
    }

    /// <summary>Cancellation from a post-filter propagates and stops later post-filters.</summary>
    [Fact]
    public async Task PostFilter_ThrowsCancellation_Propagates()
    {
        using CancellationTokenSource source = new();
        CancellingPostFilter cancellingFilter = new() { Cancel = source.Cancel };
        RecordingPostFilter  secondFilter     = new();
        _service.UseEventPostFilter(cancellingFilter);
        _service.UseEventPostFilter(secondFilter);
        await _service.StartAsync(source.Token);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                                                                 await _adapter.RaiseEventAsync(CreateMessageEvent()));

        Assert.Same(cancellingFilter.Exception, exception);
        Assert.Contains(nameof(CancellingPostFilter.OnEventProcessedAsync), exception.StackTrace);
        Assert.True(cancellingFilter.LastChainCompleted);
        Assert.Equal(1, cancellingFilter.CallCount);
        Assert.Equal(0, secondFilter.CallCount);
    }

    /// <summary>Handler cancellation terminates routing before typed handlers and post-filters.</summary>
    [Fact]
    public async Task EventHandler_Cancels_SkipsLaterHandlersAndPostFilters()
    {
        using CancellationTokenSource source = new();
        CancellationToken pipelineToken = default;
        OperationCanceledException? original = null;
        CancellationFilter preFilter = new() { Callback = ct => pipelineToken = ct };
        RecordingPostFilter postFilter = new();
        bool typedHandlerCalled = false;
        _service.UseEventPreFilter(preFilter);
        _service.UseEventPostFilter(postFilter);
        _service.Events.OnEvent += _ =>
        {
            source.Cancel();
            original = new OperationCanceledException(pipelineToken);
            throw original;
        };
        _service.Events.OnMessageReceived += _ =>
        {
            typedHandlerCalled = true;
            return ValueTask.CompletedTask;
        };
        await _service.StartAsync(source.Token);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _adapter.RaiseEventAsync(CreateMessageEvent()));

        Assert.Same(original, exception);
        Assert.Equal(pipelineToken, exception.CancellationToken);
        Assert.False(typedHandlerCalled);
        Assert.Equal(0, postFilter.CallCount);
    }

    /// <summary>A failure outside callback isolation propagates without running later stages.</summary>
    [Fact]
    public async Task PreFilter_ScopeAccessFails_SkipsLaterStages()
    {
        using CancellationTokenSource source = new();
        InvalidOperationException original = new("scope access failed");
        CancellingPostFilter postFilter = new() { Cancel = source.Cancel };
        RecordingPreFilter laterPreFilter = new();
        RecordingPostFilter laterPostFilter = new();
        bool dispatched = false;
        _service.UseEventPreFilter(new ThrowingScopePreFilter { Exception = original });
        _service.UseEventPreFilter(laterPreFilter);
        _service.UseEventPostFilter(postFilter);
        _service.UseEventPostFilter(laterPostFilter);
        _service.Events.OnMessageReceived += _ =>
        {
            dispatched = true;
            return ValueTask.CompletedTask;
        };
        await _service.StartAsync(source.Token);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _adapter.RaiseEventAsync(CreateMessageEvent()));

        Assert.Same(original, actual);
        Assert.Contains(nameof(ThrowingScopePreFilter), actual.StackTrace);
        Assert.False(dispatched);
        Assert.Equal(0, laterPreFilter.CallCount);
        Assert.Equal(0, postFilter.CallCount);
        Assert.Null(postFilter.Exception);
        Assert.Equal(0, laterPostFilter.CallCount);
    }

    /// <summary>Token identity and cancellation state determine whether a filter cancels the pipeline.</summary>
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 1, false)]
    [InlineData(false, 1, true)]
    [InlineData(false, 2, false)]
    [InlineData(false, 2, true)]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, false)]
    [InlineData(true, 2, true)]
    public async Task Filter_UnrelatedCancellation_IsIsolatedOrReplacedWithServiceCancellation(
        bool post, int tokenKind, bool cancelService)
    {
        using CancellationTokenSource source = new();
        using CancellationTokenSource externalSource = new();
        externalSource.Cancel();
        CancellationToken observedToken = default;
        OperationCanceledException? original = null;
        CancellationFilter filter = new()
        {
            Callback = ct =>
            {
                observedToken = ct;
                if (cancelService) source.Cancel();
                original = tokenKind switch
                {
                    0 => new OperationCanceledException(ct),
                    1 => new OperationCanceledException(externalSource.Token),
                    _ => new OperationCanceledException()
                };
                throw original;
            }
        };
        RecordingPreFilter secondPre = new();
        RecordingPostFilter secondPost = new();
        if (post) _service.UseEventPostFilter(filter);
        else _service.UseEventPreFilter(filter);
        _service.UseEventPreFilter(secondPre);
        _service.UseEventPostFilter(secondPost);
        await _service.StartAsync(source.Token);

        if (cancelService)
        {
            OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _adapter.RaiseEventAsync(CreateMessageEvent()));
            Assert.Equal(observedToken, exception.CancellationToken);
            Assert.NotSame(original, exception);
        }
        else
        {
            await _adapter.RaiseEventAsync(CreateMessageEvent());
        }

        Assert.Equal(!post && cancelService ? 0 : 1, secondPre.CallCount);
        Assert.Equal(cancelService ? 0 : 1, secondPost.CallCount);
    }

    /// <see cref="IEventPostFilter.OnEventProcessedAsync" />
    [Fact]
    public async Task PostFilter_ExecutesUnconditionally_EvenIfCommandMatchBlocks()
    {
        // Register a command that matches and blocks
        _service.Commands.RegisterDynamicCommand(
            async _ => await ValueTask.CompletedTask,
            ["testcmd"],
            blockAfterMatch: true);

        RecordingPostFilter postFilter = new();
        _service.UseEventPostFilter(postFilter);

        MessageReceivedEvent evt = CreateMessageEvent("testcmd");
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(1, postFilter.CallCount);
        Assert.False(postFilter.LastChainCompleted);
    }

    /// <see cref="IEventPostFilter.OnEventProcessedAsync" />
    [Fact]
    public async Task PostFilter_CanReadPipelineContext_SetByPreFilter()
    {
        _service.UseEventPreFilter(new ContextWritingPreFilter());
        ContextReadingPostFilter postFilter = new();
        _service.UseEventPostFilter(postFilter);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.True(postFilter.FoundValue);
        Assert.Equal("pre_value", postFilter.ReadValue);
    }

#endregion

#region Scope Tests

    /// <see cref="IEventPreFilter.EventTypes" />
    [Fact]
    public async Task PreFilter_WithEventTypes_OnlyMatchingTypeRuns()
    {
        RecordingPreFilter scopedFilter = new() { EventTypes = [typeof(ConnectedEvent)] };
        _service.UseEventPreFilter(scopedFilter);

        // MessageReceivedEvent should NOT trigger the filter (type mismatch)
        MessageReceivedEvent msgEvt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(msgEvt);
        Assert.Equal(0, scopedFilter.CallCount);

        // ConnectedEvent should trigger
        ConnectedEvent connectedEvt = new()
            { Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now };
        await _adapter.RaiseEventAsync(connectedEvt);
        Assert.Equal(1, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.EventTypes" />
    [Fact]
    public async Task PreFilter_WithEventTypes_BaseClassMatchesDerivedEvent()
    {
        // Setting base BotEvent should match every event type (including derived MessageReceivedEvent).
        RecordingPreFilter scopedFilter = new() { EventTypes = [typeof(BotEvent)] };
        _service.UseEventPreFilter(scopedFilter);

        MessageReceivedEvent msgEvt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(msgEvt);

        Assert.Equal(1, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.SourceTypes" />
    [Fact]
    public async Task PreFilter_WithSourceTypes_OnlyMatchingSourceRuns()
    {
        RecordingPreFilter scopedFilter = new() { SourceTypes = [MessageSourceType.Group] };
        _service.UseEventPreFilter(scopedFilter);

        // Group message → matches
        MessageReceivedEvent groupEvt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(groupEvt);
        Assert.Equal(1, scopedFilter.CallCount);

        // Friend message → mismatch
        MessageReceivedEvent friendEvt = CreateMessageEvent(sourceType: MessageSourceType.Friend);
        await _adapter.RaiseEventAsync(friendEvt);
        Assert.Equal(1, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.SourceTypes" />
    [Fact]
    public async Task PreFilter_WithSourceTypes_NonMessageEventSkipped()
    {
        // SourceTypes is set but the event is not MessageReceivedEvent → must be skipped (scope mismatch).
        RecordingPreFilter scopedFilter = new() { SourceTypes = [MessageSourceType.Group] };
        _service.UseEventPreFilter(scopedFilter);

        ConnectedEvent connectedEvt = new()
            { Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now };
        await _adapter.RaiseEventAsync(connectedEvt);

        Assert.Equal(0, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.Predicate" />
    [Fact]
    public async Task PreFilter_WithPredicateNull_NoConstraint()
    {
        RecordingPreFilter scopedFilter = new(); // Predicate not set
        _service.UseEventPreFilter(scopedFilter);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(1, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.Predicate" />
    [Fact]
    public async Task PreFilter_WithPredicateReturnsFalse_FilterSkipped()
    {
        RecordingPreFilter scopedFilter = new() { Predicate = static _ => false };
        _service.UseEventPreFilter(scopedFilter);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(0, scopedFilter.CallCount);
    }

    /// <see cref="IEventPreFilter.Predicate" />
    [Fact]
    public async Task PreFilter_WithPredicateThrows_TreatedAsScopeMismatch()
    {
        // Predicate exception is fail-safe: filter is skipped (treated as scope mismatch), pipeline continues.
        RecordingPreFilter scopedFilter = new()
        {
            Predicate = static _ => throw new InvalidOperationException("predicate boom")
        };
        _service.UseEventPreFilter(scopedFilter);

        bool dispatched = false;
        _service.Events.OnMessageReceived += async _ =>
        {
            dispatched = true;
            await ValueTask.CompletedTask;
        };

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(0, scopedFilter.CallCount);
        Assert.True(dispatched);
    }

    /// <see cref="IEventPostFilter.EventTypes" />
    [Fact]
    public async Task PostFilter_WithEventTypes_OnlyMatchingTypeRuns()
    {
        RecordingPostFilter scopedPost = new() { EventTypes = [typeof(ConnectedEvent)] };
        _service.UseEventPostFilter(scopedPost);

        MessageReceivedEvent msgEvt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(msgEvt);
        Assert.Equal(0, scopedPost.CallCount);

        ConnectedEvent connectedEvt = new()
            { Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now };
        await _adapter.RaiseEventAsync(connectedEvt);
        Assert.Equal(1, scopedPost.CallCount);
    }

    /// <see cref="IEventPostFilter.SourceTypes" />
    [Fact]
    public async Task PostFilter_WithSourceTypes_NonMessageEventSkipped()
    {
        RecordingPostFilter scopedPost = new() { SourceTypes = [MessageSourceType.Group] };
        _service.UseEventPostFilter(scopedPost);

        ConnectedEvent connectedEvt = new()
            { Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now };
        await _adapter.RaiseEventAsync(connectedEvt);

        Assert.Equal(0, scopedPost.CallCount);
    }

    /// <see cref="Sora.SoraService" />
    [Fact]
    public async Task DualInterfaceFilter_RegisteredViaBothMethods_RunsAtPreAndPost()
    {
        DualPrePostFilter filter = new();
        _service.UseEventPreFilter(filter);
        _service.UseEventPostFilter(filter);

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.Equal(1, filter.PreCallCount);
        Assert.Equal(1, filter.PostCallCount);
    }

#endregion

#region Waiter Bypass Tests

    /// <see cref="SoraService" />
    [Fact]
    public async Task WaiterConsumedEvent_BypassesAllFilters()
    {
        RecordingPreFilter  preFilter  = new();
        RecordingPostFilter postFilter = new();
        _service.UseEventPreFilter(preFilter);
        _service.UseEventPostFilter(postFilter);

        TaskCompletionSource<Task<MessageReceivedEvent?>> waiterRegistered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.Events.OnMessageReceived += e =>
        {
            Task<MessageReceivedEvent?> waiterTask = e.WaitForNextMessageAsync(TimeSpan.FromSeconds(5)).AsTask();
            waiterRegistered.TrySetResult(waiterTask);
            return ValueTask.CompletedTask;
        };

        MessageReceivedEvent sourceEvent = CreateMessageEvent("start");
        await _adapter.RaiseEventAsync(sourceEvent);

        Task<Task<MessageReceivedEvent?>> registered = waiterRegistered.Task;
        Task registeredOrTimeout = await Task.WhenAny(registered, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.Same(registered, registeredOrTimeout);
        Task<MessageReceivedEvent?> waiterTask = await registered;

        MessageReceivedEvent replyEvent = CreateMessageEvent(
            "reply",
            connectionId: sourceEvent.ConnectionId,
            senderId: sourceEvent.Message.SenderId,
            groupId: sourceEvent.Message.GroupId);
        await _adapter.RaiseEventAsync(replyEvent);

        Task completed = await Task.WhenAny(waiterTask, Task.Delay(TimeSpan.FromSeconds(5), CT));
        Assert.Same(waiterTask, completed);
        MessageReceivedEvent? result = await waiterTask;

        Assert.Same(replyEvent, result);
        Assert.Equal(1, preFilter.CallCount);
        Assert.Equal(1, postFilter.CallCount);
        Assert.Null(replyEvent.PipelineContext);
    }

    /// <summary>Event filters are frozen before adapter startup begins.</summary>
    [Fact]
    public async Task EventFilterRegistration_ClosesBeforeAdapterStart()
    {
        BlockingStartAdapter adapter = new();
        SoraService          service = new(adapter, new MockServiceConfig());
        service.UseEventPreFilter(new AlwaysPassPreFilter());
        service.UseEventPostFilter(new RecordingPostFilter());

        Task startTask = service.StartAsync(CT).AsTask();
        try
        {
            Task entered          = adapter.StartEntered.Task;
            Task enteredOrTimeout = await Task.WhenAny(entered, Task.Delay(TimeSpan.FromSeconds(5), CT));
            Assert.Same(entered, enteredOrTimeout);

            Assert.Throws<InvalidOperationException>(() => service.UseEventPreFilter(new AlwaysPassPreFilter()));
            Assert.Throws<InvalidOperationException>(() => service.UseEventPostFilter(new RecordingPostFilter()));
        }
        finally
        {
            adapter.ReleaseStart.TrySetResult(true);
            await startTask;
            await service.DisposeAsync();
        }
    }

#endregion

#region PipelineContext Initialization Tests

    /// <see cref="PipelineContext.StartTimestamp" />
    [Fact]
    public async Task PipelineContext_IsInitialized_BeforePreFilters()
    {
        bool contextInitialized = false;
        _service.UseEventPreFilter(
            new DelegatePreFilter(e =>
            {
                contextInitialized = e.PipelineContext is not null && e.PipelineContext.StartTimestamp > 0;
                return true;
            }));

        MessageReceivedEvent evt = CreateMessageEvent();
        await _adapter.RaiseEventAsync(evt);

        Assert.True(contextInitialized);
    }

#endregion

#region Test Helpers

    private static MessageReceivedEvent CreateMessageEvent(
        string            text         = "test",
        MessageSourceType sourceType   = MessageSourceType.Group,
        Guid?             connectionId = null,
        long              senderId     = 200L,
        long              groupId      = 100L) =>
        new()
        {
            Api          = null!,
            ConnectionId = connectionId ?? Guid.NewGuid(),
            SelfId       = 1L,
            Time         = DateTime.Now,
            Message = new MessageContext
            {
                MessageId  = 1,
                SourceType = sourceType,
                GroupId    = sourceType == MessageSourceType.Group ? groupId : 0L,
                SenderId   = senderId,
                Body       = new MessageBody(text)
            },
            Member = sourceType == MessageSourceType.Group
                ? new GroupMemberInfo { UserId = senderId, GroupId = groupId, Role = MemberRole.Member }
                : new GroupMemberInfo()
        };

    /// <summary>Mock adapter that implements both IBotAdapter and IAdapterEventSource for testing.</summary>
    internal sealed class MockAdapter : IBotAdapter, IAdapterEventSource
    {
        public event Func<BotEvent, ValueTask>? OnEvent;

        public string       ProtocolName => "Mock";
        public AdapterState State        => AdapterState.Running;
        public UserId       SelfId       => 1L;

        public IBotApi?        GetApi()        => null;
        public IBotConnection? GetConnection() => null;

        public ValueTask StartAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask StopAsync(CancellationToken  ct = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync()                             => ValueTask.CompletedTask;

        public async ValueTask RaiseEventAsync(BotEvent e)
        {
            if (OnEvent is not null)
                await OnEvent(e);
        }
    }

    /// <summary>Adapter whose startup remains blocked until the test releases it.</summary>
    internal sealed class BlockingStartAdapter : IBotAdapter, IAdapterEventSource
    {
        public event Func<BotEvent, ValueTask>? OnEvent;

        public TaskCompletionSource<bool> StartEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseStart { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string       ProtocolName => "BlockingMock";
        public AdapterState State        => AdapterState.Running;
        public UserId       SelfId       => 1L;

        public IBotApi?        GetApi()        => null;
        public IBotConnection? GetConnection() => null;

        public async ValueTask StartAsync(CancellationToken ct = default)
        {
            StartEntered.TrySetResult(true);
            await ReleaseStart.Task.WaitAsync(ct);
        }

        public ValueTask StopAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync()                            => ValueTask.CompletedTask;

        public async ValueTask RaiseEventAsync(BotEvent e)
        {
            if (OnEvent is not null)
                await OnEvent(e);
        }
    }

    private sealed class MockServiceConfig : IBotServiceConfig
    {
        public UserId[]        SuperUsers           => [];
        public UserId[]        BlockUsers           => [];
        public bool            AutoMarkMessageRead  => false;
        public bool            EnableCommandManager => true;
        public ILoggerFactory? LoggerFactory        => null;
        public LogLevel        MinimumLogLevel      => LogLevel.Debug;
    }

    private sealed class AlwaysPassPreFilter : IEventPreFilter
    {
        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct) => new(true);
    }

    private sealed class AlwaysBlockPreFilter : IEventPreFilter
    {
        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct) => new(false);
    }

    private sealed class ThrowingPreFilter : IEventPreFilter
    {
        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
            => throw new InvalidOperationException("pre-filter error");
    }

    /// <summary>Fails during scope evaluation, before the filter callback is entered.</summary>
    private sealed class ThrowingScopePreFilter : IEventPreFilter
    {
        public required Exception Exception { get; init; }

        public Type[]? EventTypes => throw Exception;

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct) => new(true);
    }

    private sealed class CancellingPreFilter : IEventPreFilter
    {
        public int CallCount { get; private set; }
        public Action? Cancel { get; init; }
        public OperationCanceledException? Exception { get; private set; }

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            CallCount++;
            Cancel?.Invoke();
            Exception = new OperationCanceledException("pre-filter cancellation", ct);
            throw Exception;
        }
    }

    private sealed class OrderedPreFilter(int order, List<int> executionOrder) : IEventPreFilter
    {
        public int Order => order;

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            executionOrder.Add(order);
            return new ValueTask<bool>(true);
        }
    }

    private sealed class BlockingOrderedPreFilter(int order, List<int> executionOrder) : IEventPreFilter
    {
        public int Order => order;

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            executionOrder.Add(order);
            return new ValueTask<bool>(false);
        }
    }

    private sealed class RecordingPreFilter : IEventPreFilter
    {
        public int CallCount { get; private set; }

        public Type[]?               EventTypes  { get; init; }
        public MessageSourceType[]?  SourceTypes { get; init; }
        public Func<BotEvent, bool>? Predicate   { get; init; }

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            CallCount++;
            return new ValueTask<bool>(true);
        }
    }

    private sealed class ContextWritingPreFilter : IEventPreFilter
    {
        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            e.PipelineContext?.Set("pre_key", "pre_value");
            return new ValueTask<bool>(true);
        }
    }

    private sealed class DelegatePreFilter(Func<BotEvent, bool> handler) : IEventPreFilter
    {
        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct) => new(handler(e));
    }

    private sealed class RecordingPostFilter : IEventPostFilter
    {
        public int  CallCount          { get; private set; }
        public bool LastChainCompleted { get; private set; }

        public Type[]?               EventTypes  { get; init; }
        public MessageSourceType[]?  SourceTypes { get; init; }
        public Func<BotEvent, bool>? Predicate   { get; init; }

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
        {
            CallCount++;
            LastChainCompleted = chainCompleted;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingPostFilter : IEventPostFilter
    {
        public int Order => -100;

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
            => throw new InvalidOperationException("post-filter error");
    }

    private sealed class CancellingPostFilter : IEventPostFilter
    {
        public int CallCount { get; private set; }
        public Action? Cancel { get; init; }
        public OperationCanceledException? Exception { get; private set; }
        public bool LastChainCompleted { get; private set; }

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
        {
            CallCount++;
            LastChainCompleted = chainCompleted;
            Cancel?.Invoke();
            Exception = new OperationCanceledException("post-filter cancellation", ct);
            throw Exception;
        }
    }

    /// <summary>Throws a test-selected cancellation at either event filter boundary.</summary>
    private sealed class CancellationFilter : IEventPreFilter, IEventPostFilter
    {
        public required Action<CancellationToken> Callback { get; init; }

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            Callback(ct);
            return new ValueTask<bool>(true);
        }

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
        {
            Callback(ct);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ContextReadingPostFilter : IEventPostFilter
    {
        public bool   FoundValue { get; private set; }
        public string ReadValue  { get; private set; } = "";

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
        {
            if (e.PipelineContext is not null && e.PipelineContext.TryGet("pre_key", out string val))
            {
                FoundValue = true;
                ReadValue  = val;
            }

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Filter that implements both pre and post interfaces — must be registered via both methods.</summary>
    private sealed class DualPrePostFilter : IEventPreFilter, IEventPostFilter
    {
        public int PreCallCount  { get; private set; }
        public int PostCallCount { get; private set; }

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            PreCallCount++;
            return new ValueTask<bool>(true);
        }

        public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
        {
            PostCallCount++;
            return ValueTask.CompletedTask;
        }
    }

#endregion
}
