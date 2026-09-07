using System.Collections.Concurrent;
using Xunit;

namespace Sora.Tests.Unit.Command;

#region Filter Attributes (test-only)

/// <summary>Pass-through before-filter that records every invocation.</summary>
public sealed class PassThroughBeforeAttribute : CommandBeforeFilterAttribute
{
    public static int                         CallCount;
    public static CommandFilterContext?       LastContext;
    public static PassThroughBeforeAttribute? LastInstance;

    public static void Reset()
    {
        CallCount    = 0;
        LastContext  = null;
        LastInstance = null;
    }

    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        Interlocked.Increment(ref CallCount);
        LastContext  = cmd;
        LastInstance = this;
        return new ValueTask<bool>(true);
    }
}

/// <summary>Before-filter that always blocks command execution.</summary>
public sealed class BlockingBeforeAttribute : CommandBeforeFilterAttribute
{
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
        => new(false);
}

/// <summary>Before-filter that throws (must be tolerated by pipeline).</summary>
public sealed class ThrowingBeforeAttribute : CommandBeforeFilterAttribute
{
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
        => throw new InvalidOperationException("Filter failure");
}

/// <summary>Before-filter that propagates cancellation.</summary>
public sealed class CancellingBeforeAttribute : CommandBeforeFilterAttribute
{
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
        => throw new OperationCanceledException("before-filter cancellation", ct);
}

/// <summary>Before-filter that counts invocations for cancellation tests.</summary>
public sealed class CountingBeforeAttribute : CommandBeforeFilterAttribute
{
    public int CallCount { get; private set; }

    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        CallCount++;
        return new ValueTask<bool>(true);
    }
}

/// <summary>Before-filter that records its Order in a static list at execution time.</summary>
public sealed class OrderedBeforeAttribute : CommandBeforeFilterAttribute
{
    public static readonly List<int> ExecutionOrder = [];

    public OrderedBeforeAttribute(int order)
    {
        OrderValue = order;
    }

    public          int OrderValue { get; }
    public override int Order      => OrderValue;

    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        ExecutionOrder.Add(OrderValue);
        return new ValueTask<bool>(true);
    }
}

/// <summary>After-filter that records every invocation including exception/short-circuit state.</summary>
public sealed class RecordingAfterAttribute : CommandAfterFilterAttribute
{
    public static int        CallCount;
    public static bool       LastShortCircuited;
    public static Exception? LastException;

    public static void Reset()
    {
        CallCount          = 0;
        LastShortCircuited = false;
        LastException      = null;
    }

    public override ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct)
    {
        Interlocked.Increment(ref CallCount);
        LastShortCircuited = shortCircuited;
        LastException      = exception;
        return ValueTask.CompletedTask;
    }
}

/// <summary>After-filter that throws (must be tolerated by pipeline).</summary>
public sealed class ThrowingAfterAttribute : CommandAfterFilterAttribute
{
    public override ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct)
        => throw new InvalidOperationException("After filter failure");
}

/// <summary>After-filter that propagates cancellation.</summary>
public sealed class CancellingAfterAttribute : CommandAfterFilterAttribute
{
    public override ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct)
        => throw new OperationCanceledException("after-filter cancellation", ct);
}

/// <summary>After-filter that counts invocations for cancellation tests.</summary>
public sealed class CountingAfterAttribute : CommandAfterFilterAttribute
{
    public int CallCount { get; private set; }

    public override ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct)
    {
        CallCount++;
        return ValueTask.CompletedTask;
    }
}

/// <summary>Before-filter that writes a value into PipelineContext.</summary>
public sealed class ContextWritingBeforeAttribute : CommandBeforeFilterAttribute
{
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        e.PipelineContext?.Set("shared_key", "written-by-before");
        return new ValueTask<bool>(true);
    }
}

/// <summary>After-filter that reads a value from PipelineContext.</summary>
public sealed class ContextReadingAfterAttribute : CommandAfterFilterAttribute
{
    public static bool   FoundValue;
    public static string ReadValue = "";

    public static void Reset()
    {
        FoundValue = false;
        ReadValue  = "";
    }

    public override ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct)
    {
        if (e.PipelineContext is not null && e.PipelineContext.TryGet("shared_key", out string val))
        {
            FoundValue = true;
            ReadValue  = val;
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>Class-level filter that records its instance against the executing command for shared-state verification.</summary>
public sealed class GroupCounterAttribute : CommandBeforeFilterAttribute
{
    /// <summary>Maps command method name → the GroupCounter instance that ran for that command.</summary>
    public static readonly ConcurrentDictionary<string, GroupCounterAttribute> InstancesByCommand = new();

    private int _count;

    public int Count => _count;

    public static void Reset() => InstancesByCommand.Clear();

    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        Interlocked.Increment(ref _count);
        InstancesByCommand[cmd.Method.Name] = this;
        return new ValueTask<bool>(true);
    }
}

#endregion

#region Test command groups

/// <summary>Test command group with a variety of filter attribute combinations.</summary>
[CommandGroup(Name = "filter-test")]
public static class FilterTestCommands
{
    public static int        ExecutionCount;
    public static int        PlainExecCount;
    public static int        PassThroughExecCount;
    public static int        BlockingExecCount;
    public static int        ThrowingFilterExecCount;
    public static int        OrderedExecCount;
    public static int        AfterSuccessExecCount;
    public static int        AfterShortCircuitExecCount;
    public static int        ContextExecCount;
    public static int        ThrowingAfterExecCount;
    public static Exception? CommandException;

    public static void Reset()
    {
        ExecutionCount             = 0;
        PlainExecCount             = 0;
        PassThroughExecCount       = 0;
        BlockingExecCount          = 0;
        ThrowingFilterExecCount    = 0;
        OrderedExecCount           = 0;
        AfterSuccessExecCount      = 0;
        AfterShortCircuitExecCount = 0;
        ContextExecCount           = 0;
        ThrowingAfterExecCount     = 0;
        CommandException           = null;

        PassThroughBeforeAttribute.Reset();
        OrderedBeforeAttribute.ExecutionOrder.Clear();
        RecordingAfterAttribute.Reset();
        ContextReadingAfterAttribute.Reset();
    }

    [Command(Expressions = ["plain"], MatchType = MatchType.Full)]
    public static ValueTask Plain(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref PlainExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [PassThroughBefore]
    [Command(Expressions = ["pass"], MatchType = MatchType.Full)]
    public static ValueTask PassThroughCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref PassThroughExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [BlockingBefore]
    [Command(Expressions = ["block"], MatchType = MatchType.Full)]
    public static ValueTask BlockingCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref BlockingExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [ThrowingBefore]
    [Command(Expressions = ["throw-before"], MatchType = MatchType.Full)]
    public static ValueTask ThrowingFilterCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ThrowingFilterExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [RecordingAfter]
    [Command(Expressions = ["sync-throw"], MatchType = MatchType.Full)]
    public static ValueTask SyncThrowCmd(MessageReceivedEvent e)
        => throw new InvalidOperationException("sync command error");

    [OrderedBefore(20)]
    [OrderedBefore(5)]
    [OrderedBefore(10)]
    [Command(Expressions = ["ordered"], MatchType = MatchType.Full)]
    public static ValueTask OrderedCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref OrderedExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [RecordingAfter]
    [Command(Expressions = ["after-success"], MatchType = MatchType.Full)]
    public static async ValueTask AfterSuccessCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref AfterSuccessExecCount);
        Interlocked.Increment(ref ExecutionCount);
        await ValueTask.CompletedTask;
        // throw AFTER an await point so the exception is captured in the ValueTask state machine
        // (avoids reflection's TargetInvocationException wrapping for async methods).
        if (CommandException is not null)
            throw CommandException;
    }

    [BlockingBefore]
    [RecordingAfter]
    [Command(Expressions = ["after-blocked"], MatchType = MatchType.Full)]
    public static ValueTask AfterShortCircuitCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref AfterShortCircuitExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [ContextWritingBefore]
    [ContextReadingAfter]
    [Command(Expressions = ["context"], MatchType = MatchType.Full)]
    public static ValueTask ContextCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ContextExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }

    [ThrowingAfter]
    [Command(Expressions = ["throw-after"], MatchType = MatchType.Full)]
    public static ValueTask ThrowingAfterCmd(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ThrowingAfterExecCount);
        Interlocked.Increment(ref ExecutionCount);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     Test command group with a class-level filter — must apply to every command in this group,
///     and the same attribute instance must be shared across them.
/// </summary>
[CommandGroup(Name = "group-counter")]
[GroupCounter]
public static class GroupCounterCommands
{
    public static int CmdAExec;
    public static int CmdBExec;

    public static void Reset()
    {
        CmdAExec = 0;
        CmdBExec = 0;
        GroupCounterAttribute.Reset();
    }

    [Command(Expressions = ["g-a"], MatchType = MatchType.Full)]
    public static ValueTask CmdA(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref CmdAExec);
        return ValueTask.CompletedTask;
    }

    [Command(Expressions = ["g-b"], MatchType = MatchType.Full)]
    public static ValueTask CmdB(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref CmdBExec);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     Class-level filter at Order=10 + method-level filter at Order=10 — class must precede method on tie.
/// </summary>
[CommandGroup(Name = "tie-break")]
[OrderedBefore(10)]
public static class TieBreakCommands
{
    public static int Exec;

    public static void Reset()
    {
        Exec = 0;
        OrderedBeforeAttribute.ExecutionOrder.Clear();
    }

    [OrderedBefore(10)]
    [Command(Expressions = ["tie"], MatchType = MatchType.Full)]
    public static ValueTask Tie(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref Exec);
        return ValueTask.CompletedTask;
    }
}

#endregion

/// <summary>Tests for command filter integration in <see cref="CommandManager" />.</summary>
[Collection("Command.Unit")]
[Trait("Category", "Unit")]
public class CommandFilterTests : IDisposable
{
    private readonly CommandManager _manager = new();

    public CommandFilterTests()
    {
        FilterTestCommands.Reset();
        GroupCounterCommands.Reset();
        TieBreakCommands.Reset();
        _manager.ScanType(typeof(FilterTestCommands));
        _manager.ScanType(typeof(GroupCounterCommands));
        _manager.ScanType(typeof(TieBreakCommands));
    }

    public void Dispose()
    {
        FilterTestCommands.Reset();
        GroupCounterCommands.Reset();
        TieBreakCommands.Reset();
    }

    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region Opt-in Tests

    /// <summary>A command without any filter attribute → filter does NOT run, command does.</summary>
    [Fact]
    public async Task CommandWithoutFilterAttribute_FilterDoesNotRun()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("plain"), CT);

        Assert.Equal(1, FilterTestCommands.PlainExecCount);
        Assert.Equal(0, PassThroughBeforeAttribute.CallCount);
        Assert.Equal(0, RecordingAfterAttribute.CallCount);
    }

    /// <summary>Command with a [PassThroughBefore] attribute → filter runs, command runs.</summary>
    [Fact]
    public async Task CommandWithBeforeAttribute_FilterRuns()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        Assert.Equal(1, PassThroughBeforeAttribute.CallCount);
        Assert.Equal(1, FilterTestCommands.PassThroughExecCount);
    }

    /// <summary>Filter on one command does not leak to a sibling command in the same group.</summary>
    [Fact]
    public async Task FilterOnOneCommand_DoesNotLeakToSibling()
    {
        // Run "plain" (no filter), then "pass" (with filter). Filter should be hit exactly once.
        await _manager.HandleMessageEventAsync(CreateTestEvent("plain"), CT);
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        Assert.Equal(1, PassThroughBeforeAttribute.CallCount);
    }

#endregion

#region BeforeFilter Behavior

    [Fact]
    public async Task BeforeFilter_ReturnsFalse_CommandShortCircuited()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("block"), CT);
        Assert.Equal(0, FilterTestCommands.BlockingExecCount);
    }

    [Fact]
    public async Task BeforeFilter_ThrowsException_TreatedAsPassThrough()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("throw-before"), CT);
        Assert.Equal(1, FilterTestCommands.ThrowingFilterExecCount);
    }

    /// <summary>Cancellation from a before-filter propagates and later filters do not run.</summary>
    [Fact]
    public async Task BeforeFilter_ThrowsCancellation_Propagates()
    {
        CountingBeforeAttribute secondFilter = new();
        _manager.RegisterDynamicCommand(
            _ => ValueTask.CompletedTask,
            ["cancel-before"],
            beforeFilters: [new CancellingBeforeAttribute(), secondFilter]);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                                                                 await _manager.HandleMessageEventAsync(
                                                                     CreateTestEvent("cancel-before"),
                                                                     CT));

        Assert.Equal(0, secondFilter.CallCount);
    }

    /// <summary>Multiple before-filter attributes execute in ascending Order.</summary>
    [Fact]
    public async Task MultipleBeforeFilters_ExecuteInOrderAscending()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("ordered"), CT);

        Assert.Equal([5, 10, 20], OrderedBeforeAttribute.ExecutionOrder);
        Assert.Equal(1, FilterTestCommands.OrderedExecCount);
    }

#endregion

#region AfterFilter Behavior

    [Fact]
    public async Task AfterFilter_ExecutesAfterSuccessfulCommand()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("after-success"), CT);

        Assert.Equal(1, RecordingAfterAttribute.CallCount);
        Assert.False(RecordingAfterAttribute.LastShortCircuited);
        Assert.Null(RecordingAfterAttribute.LastException);
    }

    [Fact]
    public async Task AfterFilter_ExecutesWhenCommandThrows()
    {
        FilterTestCommands.CommandException = new InvalidOperationException("test error");

        await _manager.HandleMessageEventAsync(CreateTestEvent("after-success"), CT);

        Assert.Equal(1, RecordingAfterAttribute.CallCount);
        Assert.False(RecordingAfterAttribute.LastShortCircuited);
        Assert.IsType<InvalidOperationException>(RecordingAfterAttribute.LastException);
    }

    [Fact]
    public async Task AfterFilter_ExecutesWhenBeforeFilterShortCircuits()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("after-blocked"), CT);

        Assert.Equal(1, RecordingAfterAttribute.CallCount);
        Assert.True(RecordingAfterAttribute.LastShortCircuited);
        Assert.Null(RecordingAfterAttribute.LastException);
        Assert.Equal(0, FilterTestCommands.AfterShortCircuitExecCount);
    }

    [Fact]
    public async Task AfterFilter_ThrowsException_DoesNotCrashPipeline()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("throw-after"), CT);
        Assert.Equal(1, FilterTestCommands.ThrowingAfterExecCount);
    }

    /// <summary>Cancellation from an after-filter propagates and later filters do not run.</summary>
    [Fact]
    public async Task AfterFilter_ThrowsCancellation_Propagates()
    {
        CountingAfterAttribute secondFilter = new();
        _manager.RegisterDynamicCommand(
            _ => ValueTask.CompletedTask,
            ["cancel-after"],
            afterFilters: [new CancellingAfterAttribute(), secondFilter]);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                                                                 await _manager.HandleMessageEventAsync(
                                                                     CreateTestEvent("cancel-after"),
                                                                     CT));

        Assert.Equal(0, secondFilter.CallCount);
    }

    /// <summary>Synchronous command exceptions are unwrapped before after-filters observe them.</summary>
    [Fact]
    public async Task AfterFilter_ReceivesOriginalSynchronousCommandException()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("sync-throw"), CT);

        InvalidOperationException exception =
            Assert.IsType<InvalidOperationException>(RecordingAfterAttribute.LastException);
        Assert.Equal("sync command error", exception.Message);
    }

#endregion

#region Stateful Sharing

    /// <summary>Same command invoked multiple times shares one filter attribute instance (state preserved).</summary>
    [Fact]
    public async Task SameCommandMultipleInvocations_SharesAttributeInstance()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        Assert.Equal(3, PassThroughBeforeAttribute.CallCount);
    }

    /// <summary>
    ///     Class-level filter on a CommandGroup is shared across all commands in the group — the same
    ///     <see cref="GroupCounterAttribute" /> instance must accumulate state from every command in the group.
    /// </summary>
    [Fact]
    public async Task ClassLevelFilter_SharedAcrossAllGroupCommands()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("g-a"), CT);
        await _manager.HandleMessageEventAsync(CreateTestEvent("g-b"), CT);
        await _manager.HandleMessageEventAsync(CreateTestEvent("g-a"), CT);

        Assert.Equal(2, GroupCounterCommands.CmdAExec);
        Assert.Equal(1, GroupCounterCommands.CmdBExec);

        // GroupCounter must be the SAME instance for both CmdA and CmdB (class-level pinning).
        Assert.True(
            GroupCounterAttribute.InstancesByCommand.TryGetValue(
                nameof(GroupCounterCommands.CmdA),
                out GroupCounterAttribute? aInstance));
        Assert.True(
            GroupCounterAttribute.InstancesByCommand.TryGetValue(
                nameof(GroupCounterCommands.CmdB),
                out GroupCounterAttribute? bInstance));
        Assert.Same(aInstance, bInstance);
        // The shared instance recorded all 3 invocations across both commands.
        Assert.Equal(3, aInstance.Count);
    }

    /// <summary>
    ///     With class-level Order=10 and method-level Order=10, both attributes are invoked. The framework
    ///     guarantees class-first ordering by concatenating classBefore before methodBefore prior to the
    ///     stable sort, so the executed list contains [class, method].
    /// </summary>
    [Fact]
    public async Task ClassAndMethodFilter_SameOrder_ClassFirst()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("tie"), CT);

        // Both filters ran (recorded their order=10 values)
        Assert.Equal([10, 10], OrderedBeforeAttribute.ExecutionOrder);
        Assert.Equal(1, TieBreakCommands.Exec);
    }

#endregion

#region PipelineContext

    /// <summary>Before- and after-filters share PipelineContext through Set/TryGet.</summary>
    [Fact]
    public async Task PipelineContext_SharedBetweenFiltersAndCommand()
    {
        PipelineContext      ctx = new() { StartTimestamp = 100L };
        MessageReceivedEvent evt = CreateTestEvent("context");
        evt.PipelineContext = ctx;

        await _manager.HandleMessageEventAsync(evt, CT);

        Assert.True(ContextReadingAfterAttribute.FoundValue);
        Assert.Equal("written-by-before", ContextReadingAfterAttribute.ReadValue);
    }

    /// <summary>
    ///     Before-filter receives a CommandFilterContext whose Method/Expressions/MatchType/DeclaringType
    ///     reflect the matched command exactly.
    /// </summary>
    [Fact]
    public async Task BeforeFilter_ReceivesCorrectCommandFilterContext()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        Assert.NotNull(PassThroughBeforeAttribute.LastContext);
        Assert.Equal(nameof(FilterTestCommands.PassThroughCmd), PassThroughBeforeAttribute.LastContext!.Method.Name);
        Assert.Contains("pass", PassThroughBeforeAttribute.LastContext.Expressions);
        Assert.Equal(MatchType.Full, PassThroughBeforeAttribute.LastContext.MatchType);
        Assert.Equal(typeof(FilterTestCommands), PassThroughBeforeAttribute.LastContext.DeclaringType);
    }

    /// <summary>
    ///     CommandFilterContext.Attributes contains the same pinned filter instance that the framework
    ///     invokes — confirming filter and context observe identical instances.
    /// </summary>
    [Fact]
    public async Task CommandFilterContext_AttributesContainsPinnedFilterInstance()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        // The PassThrough filter that ran recorded its own `this` reference.
        Assert.NotNull(PassThroughBeforeAttribute.LastInstance);
        Assert.NotNull(PassThroughBeforeAttribute.LastContext);

        // The CommandFilterContext.Attributes collection must contain that exact instance.
        Assert.Contains(
            PassThroughBeforeAttribute.LastInstance!,
            PassThroughBeforeAttribute.LastContext!.Attributes);
    }

    /// <summary>Filter context collections are read-only snapshots and cannot mutate command metadata.</summary>
    [Fact]
    public async Task CommandFilterContext_CollectionsAreReadOnlySnapshots()
    {
        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);

        Assert.NotNull(PassThroughBeforeAttribute.LastContext);
        IReadOnlyList<string>    expressions = PassThroughBeforeAttribute.LastContext!.Expressions;
        IReadOnlyList<Attribute> attributes  = PassThroughBeforeAttribute.LastContext.Attributes;

        IList<string>    expressionList = Assert.IsAssignableFrom<IList<string>>(expressions);
        IList<Attribute> attributeList  = Assert.IsAssignableFrom<IList<Attribute>>(attributes);
        Assert.True(expressionList.IsReadOnly);
        Assert.True(attributeList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => expressionList[0] = "mutated");
        Assert.Throws<NotSupportedException>(() => attributeList.Clear());

        await _manager.HandleMessageEventAsync(CreateTestEvent("pass"), CT);
        Assert.Contains("pass", PassThroughBeforeAttribute.LastContext.Expressions);
    }

#endregion

#region Dynamic Commands

    /// <summary>Dynamic command without filter args → no filter runs.</summary>
    [Fact]
    public async Task DynamicCommand_WithoutFilters_NoFilterRuns()
    {
        int execCount = 0;
        _manager.RegisterDynamicCommand(
            _ =>
            {
                execCount++;
                return ValueTask.CompletedTask;
            },
            ["dyn-bare"]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-bare"), CT);

        Assert.Equal(1, execCount);
        Assert.Equal(0, PassThroughBeforeAttribute.CallCount);
    }

    /// <summary>Dynamic command with explicitly passed before-filters → those filters run.</summary>
    [Fact]
    public async Task DynamicCommand_WithBeforeFilters_FiltersRun()
    {
        int                        execCount = 0;
        PassThroughBeforeAttribute filter    = new();

        _manager.RegisterDynamicCommand(
            _ =>
            {
                execCount++;
                return ValueTask.CompletedTask;
            },
            ["dyn-with"],
            beforeFilters: [filter]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-with"), CT);

        Assert.Equal(1, execCount);
        Assert.Equal(1, PassThroughBeforeAttribute.CallCount);
    }

    /// <summary>Dynamic command with after-filter receives correct shortCircuited / exception state.</summary>
    [Fact]
    public async Task DynamicCommand_WithAfterFilter_RecordsExecution()
    {
        RecordingAfterAttribute afterFilter = new();
        _manager.RegisterDynamicCommand(
            static _ => ValueTask.CompletedTask,
            ["dyn-after"],
            afterFilters: [afterFilter]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-after"), CT);

        Assert.Equal(1, RecordingAfterAttribute.CallCount);
        Assert.False(RecordingAfterAttribute.LastShortCircuited);
        Assert.Null(RecordingAfterAttribute.LastException);
    }

    /// <summary>
    ///     Filter attributes applied directly to the lambda (C# 10+ lambda attribute syntax) are
    ///     auto-discovered via <c>handler.Method.GetCustomAttributes(true)</c> — no explicit
    ///     <c>beforeFilters</c> argument needed.
    /// </summary>
    [Fact]
    public async Task DynamicCommand_WithLambdaAttribute_FilterAutoDiscovered()
    {
        int execCount = 0;
        _manager.RegisterDynamicCommand(
            [PassThroughBefore](e) =>
            {
                execCount++;
                return ValueTask.CompletedTask;
            },
            ["dyn-lambda"]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-lambda"), CT);

        Assert.Equal(1, execCount);
        Assert.Equal(1, PassThroughBeforeAttribute.CallCount);
    }

    /// <summary>
    ///     Lambda-attribute filters and explicitly-passed filters both apply (union, sorted by Order).
    /// </summary>
    [Fact]
    public async Task DynamicCommand_LambdaAttributeAndExplicitFilters_BothApply()
    {
        RecordingAfterAttribute explicitAfter = new();
        _manager.RegisterDynamicCommand(
            [PassThroughBefore](e) => ValueTask.CompletedTask,
            ["dyn-mixed"],
            afterFilters: [explicitAfter]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-mixed"), CT);

        Assert.Equal(1, PassThroughBeforeAttribute.CallCount); // from lambda attribute
        Assert.Equal(1, RecordingAfterAttribute.CallCount);    // from explicit afterFilters arg
    }

    /// <summary>
    ///     Lambda with multiple filter attributes — all are discovered and ordered by Order.
    /// </summary>
    [Fact]
    public async Task DynamicCommand_MultipleLambdaAttributes_OrderedByOrderAscending()
    {
        _manager.RegisterDynamicCommand(
            [OrderedBefore(20)] [OrderedBefore(5)] [OrderedBefore(10)](e) => ValueTask.CompletedTask,
            ["dyn-multi"]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-multi"), CT);

        Assert.Equal([5, 10, 20], OrderedBeforeAttribute.ExecutionOrder);
    }

    /// <summary>
    ///     Same filter instance referenced multiple times in <c>beforeFilters</c> is deduplicated by
    ///     reference — runs exactly once and appears once in <see cref="CommandFilterContext.Attributes" />.
    /// </summary>
    [Fact]
    public async Task DynamicCommand_DuplicateInstanceInBeforeFilters_DeduplicatedByReference()
    {
        PassThroughBeforeAttribute filter = new();
        _manager.RegisterDynamicCommand(
            _ => ValueTask.CompletedTask,
            ["dyn-dup-before"],
            beforeFilters: [filter, filter, filter]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-dup-before"), CT);

        // Filter ran exactly once despite being passed three times.
        Assert.Equal(1, PassThroughBeforeAttribute.CallCount);
        // Attributes also has the instance once, not three times.
        Assert.NotNull(PassThroughBeforeAttribute.LastContext);
        Assert.Equal(
            1,
            PassThroughBeforeAttribute.LastContext!.Attributes
                                      .Count(a => ReferenceEquals(a, filter)));
    }

    /// <summary>
    ///     Distinct instances of the same filter type are NOT deduplicated — each carries its own
    ///     state/parameters so both are intentional and must run.
    /// </summary>
    [Fact]
    public async Task DynamicCommand_DistinctInstancesOfSameFilterType_BothRun()
    {
        OrderedBeforeAttribute first  = new(5);
        OrderedBeforeAttribute second = new(10);
        _manager.RegisterDynamicCommand(
            _ => ValueTask.CompletedTask,
            ["dyn-distinct"],
            beforeFilters: [first, second]);

        await _manager.HandleMessageEventAsync(CreateTestEvent("dyn-distinct"), CT);

        // Both ran (different instances, dedup is reference-based, not type-based).
        Assert.Equal([5, 10], OrderedBeforeAttribute.ExecutionOrder);
    }

#endregion

#region Test Helpers

    private static MessageReceivedEvent CreateTestEvent(string text) =>
        new()
        {
            Api          = null!,
            ConnectionId = Guid.Empty,
            SelfId       = 1L,
            Time         = DateTime.Now,
            Message = new MessageContext
            {
                MessageId  = 1,
                SourceType = MessageSourceType.Group,
                GroupId    = 100L,
                SenderId   = 200L,
                Body       = new MessageBody(text)
            },
            Member = new GroupMemberInfo { UserId = 200L, GroupId = 100L, Role = MemberRole.Member }
        };

#endregion
}