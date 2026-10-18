using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Sora.Command.InternalEntities;
using Sora.Tests.CommandFixture;
using Xunit;

namespace Sora.Tests.Unit.Command;

/// <summary>Test-assembly command type with two commands of different priorities.</summary>
public sealed class AssemblyOrderCommands
{
    private readonly Action<string> _record;

    /// <summary>Creates the group with an execution recorder.</summary>
    public AssemblyOrderCommands(Action<string> record)
    {
        _record = record;
    }

    /// <see cref="CommandAttribute.Priority" />
    [Command(Expressions = ["order"], MatchType = MatchType.Keyword, Priority = 1, BlockAfterMatch = false)]
    public ValueTask A1(MessageReceivedEvent e)
    {
        _record("A1");
        return ValueTask.CompletedTask;
    }

    /// <see cref="CommandAttribute.Priority" />
    [Command(Expressions = ["order"], MatchType = MatchType.Keyword, Priority = 100, BlockAfterMatch = false)]
    public ValueTask A2(MessageReceivedEvent e)
    {
        _record("A2");
        return ValueTask.CompletedTask;
    }
}

/// <summary>A second test-assembly command type used for type-level ordering and enabling.</summary>
public sealed class SecondOrderCommands
{
    private readonly Action<string> _record;

    /// <summary>Creates the group with an execution recorder.</summary>
    public SecondOrderCommands(Action<string> record)
    {
        _record = record;
    }

    /// <see cref="CommandAttribute.Priority" />
    [Command(Expressions = ["order"], MatchType = MatchType.Keyword, Priority = 50, BlockAfterMatch = false)]
    public ValueTask T2(MessageReceivedEvent e)
    {
        _record("T2");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Test-assembly type inheriting its command from the fixture assembly.</summary>
public sealed class DerivedPluginCommands : FixtureCommandBase
{
    /// <summary>Creates the derived group with an execution recorder.</summary>
    public DerivedPluginCommands(Action<string> record)
        : base(record)
    {
    }
}

/// <summary>Command type whose constructor fails during scanning.</summary>
public sealed class ThrowingConstructorCommands
{
    /// <summary>Always throws.</summary>
    public ThrowingConstructorCommands()
    {
        throw new InvalidOperationException("constructor failure");
    }

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Command(Expressions = ["ctor-static"])]
    public static ValueTask Static(MessageReceivedEvent e) => ValueTask.CompletedTask;

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Command(Expressions = ["ctor-instance"])]
    public ValueTask Instance(MessageReceivedEvent e) => ValueTask.CompletedTask;
}

/// <summary>Abstract command type that cannot be scanned directly.</summary>
public abstract class AbstractLocalCommands
{
    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Command(Expressions = ["abstract-local"])]
    public ValueTask Instance(MessageReceivedEvent e) => ValueTask.CompletedTask;
}

/// <summary>Open generic command type that cannot be scanned directly.</summary>
// ReSharper disable once UnusedTypeParameter
public sealed class GenericLocalCommands<T>
{
    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Command(Expressions = ["generic-local"])]
    public static ValueTask Static(MessageReceivedEvent e) => ValueTask.CompletedTask;
}

/// <summary>Before-filter counting invocations; used to verify filter state survives disabling.</summary>
public sealed class ManagementCountingBeforeAttribute : CommandBeforeFilterAttribute
{
    private int _count;

    /// <summary>The number of invocations.</summary>
    public int Count => Volatile.Read(ref _count);

    /// <inheritdoc />
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        Interlocked.Increment(ref _count);
        return ValueTask.FromResult(true);
    }
}

/// <summary>Before-filter that short-circuits every command.</summary>
public sealed class ManagementBlockingBeforeAttribute : CommandBeforeFilterAttribute
{
    /// <inheritdoc />
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct) =>
        ValueTask.FromResult(false);
}

/// <summary>Before-filter that waits until the Sora token is canceled.</summary>
public sealed class ManagementWaitForCancellationBeforeAttribute : CommandBeforeFilterAttribute
{
    /// <summary>Completes when the filter starts waiting.</summary>
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public override async ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        Started.TrySetResult();
        await Task.Delay(Timeout.Infinite, ct);
        return true;
    }
}

/// <summary>After-filter that waits for a gate before completing.</summary>
public sealed class ManagementGatedAfterAttribute : CommandAfterFilterAttribute
{
    /// <summary>Completes when the filter starts.</summary>
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Releases the filter.</summary>
    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public override async ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           commandException,
        CancellationToken    ct)
    {
        Started.TrySetResult();
        await Gate.Task.WaitAsync(ct);
    }
}

/// <summary>
///     Tests for <see cref="CommandManager.RegisterAssembly" />, <see cref="AssemblyRegistration" /> and
///     <see cref="ICommandRegistrar" />: two-level ordering, enabling, querying and unregistration across two real
///     assemblies (this test assembly and <c>Sora.Tests.CommandFixture</c>).
/// </summary>
[Collection("Command.Unit")]
[Trait("Category", "Unit")]
public class CommandAssemblyManagementTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentQueue<string> _records = new();

    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private static Assembly TestAssembly => typeof(CommandAssemblyManagementTests).Assembly;

    private static Assembly FixtureAssembly => typeof(FixtureOrderCommands).Assembly;

#region Two-level Ordering Tests

    /// <see cref="AssemblyRegistration.Priority" />
    [Fact]
    public async Task AssemblyPriority_PrecedesCommandPriority()
    {
        CommandManager manager = new();
        RegisterA(manager, 10);
        RegisterB(manager, 5);

        await SendAsync(manager, "order");

        Assert.Equal(["A2", "A1", "B1"], _records);
    }

    /// <see cref="CommandManager.RegisterAssembly" />
    [Fact]
    public async Task EqualAssemblyPriority_KeepsRegistrationOrderWithoutInterleaving()
    {
        CommandManager manager = new();
        RegisterB(manager, 0);
        RegisterA(manager, 0);

        await SendAsync(manager, "order");

        Assert.Equal(["B1", "A2", "A1"], _records);
    }

    /// <see cref="ICommandRegistrar.RegisterDynamicCommand" />
    [Fact]
    public async Task DynamicAndScannedCommands_MixInsideAssembly()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 0);
        AddRecordingCommand(a, typeof(AssemblyOrderCommands), "D1", 50);
        AddRecordingCommand(a, typeof(AssemblyOrderCommands), "D2", 100);

        await SendAsync(manager, "order");

        Assert.Equal(["A2", "D2", "D1", "A1"], _records);
    }

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Fact]
    public async Task MultipleTypes_SortByCommandPriorityInsideAssembly()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 0);
        RegisterSecondType(a);

        await SendAsync(manager, "order");

        Assert.Equal(["A2", "T2", "A1"], _records);
    }

    /// <see cref="AssemblyRegistration.SetPriority" />
    [Fact]
    public async Task SetPriority_ChangesNextMessageOrder()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 10);
        AssemblyRegistration b       = RegisterB(manager, 5);

        Assert.True(b.SetPriority(20));
        await SendAsync(manager, "order");
        Assert.Equal(["B1", "A2", "A1"], DrainRecords());

        Assert.True(a.SetPriority(10));
        Assert.True(b.SetPriority(20));
        await SendAsync(manager, "order");
        Assert.Equal(["B1", "A2", "A1"], DrainRecords());

        Assert.True(b.SetPriority(10));
        await SendAsync(manager, "order");
        Assert.Equal(["A2", "A1", "B1"], DrainRecords());
        Assert.Equal(10, b.Priority);
    }

#endregion

#region Enable Tests

    /// <see cref="AssemblyRegistration.SetEnabled" />
    [Fact]
    public async Task AssemblyAndTypeEnabled_CombineAndKeepLocalState()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 10);
        RegisterSecondType(a);
        RegisterB(manager, 5);

        Assert.True(a.SetTypeEnabled(typeof(SecondOrderCommands), false));
        Assert.True(a.SetEnabled(false));
        await SendAsync(manager, "order");
        Assert.Equal(["B1"], DrainRecords());

        Assert.True(a.SetEnabled(true));
        await SendAsync(manager, "order");
        Assert.Equal(["A2", "A1", "B1"], DrainRecords());

        Assert.True(a.SetTypeEnabled(typeof(SecondOrderCommands), true));
        await SendAsync(manager, "order");
        Assert.Equal(["A2", "T2", "A1", "B1"], DrainRecords());
    }

    /// <see cref="ICommandRegistrar.RegisterDynamicCommand" />
    [Fact]
    public async Task DynamicCommands_FollowAssemblyAndOwnerTypeState()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = manager.RegisterAssembly(TestAssembly);
        Guid                 kept    = AddRecordingCommand(a, typeof(SecondOrderCommands), "Dyn", 0);

        Assert.True(a.SetEnabled(false));
        Guid whileDisabled = AddRecordingCommand(a, typeof(SecondOrderCommands), "Late", 0);
        await SendAsync(manager, "order");
        Assert.Empty(DrainRecords());

        Assert.True(a.Registrar.UnregisterDynamicCommand(whileDisabled));
        Assert.True(a.SetEnabled(true));
        await SendAsync(manager, "order");
        Assert.Equal(["Dyn"], DrainRecords());

        Assert.True(a.SetTypeEnabled(typeof(SecondOrderCommands), false));
        await SendAsync(manager, "order");
        Assert.Empty(DrainRecords());

        Assert.True(a.SetTypeEnabled(typeof(SecondOrderCommands), true));
        await SendAsync(manager, "order");
        Assert.Equal(["Dyn"], DrainRecords());

        AssemblyRegistration b = RegisterB(manager, 0);
        Assert.False(b.Registrar.UnregisterDynamicCommand(kept));
        Assert.True(a.Registrar.UnregisterDynamicCommand(kept));
    }

    /// <see cref="AssemblyRegistration.SetEnabled" />
    [Fact]
    public async Task DisableRestore_KeepsInstanceAndFilterState()
    {
        CommandManager                    manager = new();
        AssemblyRegistration              a       = RegisterA(manager, 0);
        ManagementCountingBeforeAttribute counter = new();
        a.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ => ValueTask.CompletedTask,
            ["count"],
            beforeFilters: [counter]);
        AssemblyOrderCommands? instance = manager.GetCommandInstance<AssemblyOrderCommands>();

        await SendAsync(manager, "count");
        Assert.True(a.SetEnabled(false));
        await SendAsync(manager, "count");
        Assert.True(a.SetEnabled(true));
        await SendAsync(manager, "count");

        Assert.Equal(2, counter.Count);
        Assert.NotNull(instance);
        Assert.Same(instance, manager.GetCommandInstance<AssemblyOrderCommands>());
    }

#endregion

#region Execution Boundary Tests

    /// <see cref="AssemblyRegistration.SetEnabled" />
    [Fact]
    public async Task ConfigurationChangeDuringMessage_AppliesToNextMessage()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = manager.RegisterAssembly(TestAssembly, 10);
        AssemblyRegistration b       = RegisterB(manager, 5);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate    = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AddGatedCommand(a, "Gate", started, gate);

        Task running = SendAsync(manager, "order").AsTask();
        await started.Task.WaitAsync(Timeout, CT);
        Assert.True(b.SetEnabled(false));
        gate.TrySetResult();
        await running.WaitAsync(Timeout, CT);
        Assert.Equal(["Gate", "B1"], DrainRecords());

        await SendAsync(manager, "order");
        Assert.Equal(["Gate"], DrainRecords());
    }

    /// <see cref="CommandAfterFilterAttribute" />
    [Fact]
    public async Task NextAssemblyStarts_OnlyAfterPreviousAfterFilterCompletes()
    {
        CommandManager                manager = new();
        AssemblyRegistration          a       = manager.RegisterAssembly(TestAssembly, 10);
        ManagementGatedAfterAttribute after   = new();
        RegisterB(manager, 5);
        a.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ =>
            {
                _records.Enqueue("A");
                return ValueTask.CompletedTask;
            },
            ["order"],
            MatchType.Keyword,
            blockAfterMatch: false,
            afterFilters: [after]);

        Task running = SendAsync(manager, "order").AsTask();
        await after.Started.Task.WaitAsync(Timeout, CT);
        Assert.Equal(["A"], _records);

        after.Gate.TrySetResult();
        await running.WaitAsync(Timeout, CT);
        Assert.Equal(["A", "B1"], _records);
    }

    /// <see cref="CommandAttribute.BlockAfterMatch" />
    [Fact]
    public async Task GlobalBlock_StopsRemainingAndLaterAssemblies()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 10);
        RegisterB(manager, 5);
        a.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ =>
            {
                _records.Enqueue("Block");
                return ValueTask.CompletedTask;
            },
            ["order"],
            MatchType.Keyword,
            priority: 200);

        await SendAsync(manager, "order");

        Assert.Equal(["Block"], _records);
    }

#endregion

#region Ownership Tests

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Fact]
    public async Task InheritedCommand_BelongsToScannedDerivedType()
    {
        CommandManager       manager = new();
        AssemblyRegistration local   = manager.RegisterAssembly(TestAssembly);
        local.RegisterCommandInstance(new DerivedPluginCommands(_records.Enqueue));
        Assert.Equal(1, local.ScanType(typeof(DerivedPluginCommands)));
        AssemblyRegistration fixture = manager.RegisterAssembly(FixtureAssembly);
        Assert.True(fixture.SetEnabled(false));

        await SendAsync(manager, "inherited");

        Assert.Equal(["Inherited"], _records);
        CommandAssemblyState state = Assert.Single(
            manager.GetAssemblyStates(),
            assemblyState => assemblyState.Assembly == TestAssembly);
        CommandTypeState typeState = Assert.Single(state.Types);
        Assert.Equal(typeof(DerivedPluginCommands), typeState.Type);
    }

    /// <see cref="ICommandRegistrar.RegisterDynamicCommand" />
    [Fact]
    public void ForeignTypes_AreRejected()
    {
        CommandManager       manager = new();
        AssemblyRegistration local   = manager.RegisterAssembly(TestAssembly);

        Assert.Throws<ArgumentException>(() => local.ScanType(typeof(FixtureOrderCommands)));
        Assert.Throws<ArgumentException>(() => local.RegisterCommandInstance(new FixtureOrderCommands(_ => { })));
        Assert.Throws<ArgumentException>(() => RegisterNoopCommand(local, typeof(FixtureCommandBase)));
        Assert.Throws<ArgumentException>(() => RegisterNoopCommand(local, typeof(CommandManager)));
        Assert.Throws<ArgumentException>(() => RegisterNoopCommand(local, typeof(object)));
        Assert.False(local.SetTypeEnabled(typeof(FixtureOrderCommands), false));
        Assert.False(local.SetTypeEnabled(typeof(SecondOrderCommands), false));

        RegisterNoopCommand(local, typeof(DerivedPluginCommands));
        Assert.Equal(typeof(DerivedPluginCommands), Assert.Single(manager.GetAssemblyStates()[0].Types).Type);
    }

    /// <see cref="AssemblyRegistration.ScanCommandGroups" />
    [Fact]
    public void ScanCommandGroups_SkipsAbstractAndOpenGenericTypes()
    {
        CommandManager       manager = new();
        AssemblyRegistration fixture = manager.RegisterAssembly(FixtureAssembly);
        fixture.RegisterCommandInstance(new FixtureOrderCommands(_records.Enqueue));

        Assert.Equal(2, fixture.ScanCommandGroups());

        Type[] types = [.. manager.GetAssemblyStates()[0].Types.Select(typeState => typeState.Type)];
        Assert.Equal(2, types.Length);
        Assert.Contains(typeof(FixtureOrderCommands), types);
        Assert.Contains(typeof(FixtureStaticCommands), types);
    }

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Fact]
    public void ScanType_RejectsAbstractAndOpenGenericTypes()
    {
        CommandManager       manager = new();
        AssemblyRegistration local   = manager.RegisterAssembly(TestAssembly);

        Assert.Throws<ArgumentException>(() => local.ScanType(typeof(AbstractLocalCommands)));
        Assert.Throws<ArgumentException>(() => local.ScanType(typeof(GenericLocalCommands<>)));
    }

#endregion

#region Registration Boundary Tests

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Fact]
    public async Task DuplicateScanAndRegistration_DoNotDuplicateOrReset()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 3);

        Assert.Equal(0, a.ScanType(typeof(AssemblyOrderCommands)));
        AssemblyRegistration again = manager.RegisterAssembly(TestAssembly, 99, false);
        await SendAsync(manager, "order");

        Assert.Same(a, again);
        Assert.Equal(3, again.Priority);
        Assert.True(again.IsEnabled);
        Assert.Equal(["A2", "A1"], _records);
    }

    /// <see cref="AssemblyRegistration.ScanType(Type)" />
    [Fact]
    public async Task FailedScan_PublishesNothing()
    {
        CommandManager       manager = new();
        AssemblyRegistration local   = manager.RegisterAssembly(TestAssembly);

        Assert.Throws<TargetInvocationException>(() => local.ScanType(typeof(ThrowingConstructorCommands)));
        Assert.Throws<TargetInvocationException>(() => local.ScanType(typeof(ThrowingConstructorCommands)));

        Assert.Empty(manager.GetAssemblyStates()[0].Types);
        Assert.Null(manager.GetCommandInstance<ThrowingConstructorCommands>());
        MessageReceivedEvent evt = CreateEvent("ctor-static");
        await manager.HandleMessageEventAsync(evt, CT);
        Assert.True(evt.IsContinueEventChain);
    }

    /// <see cref="CommandManager.RegisterAssembly" />
    [Fact]
    public async Task InitialValues_ApplyToNewRegistration()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = manager.RegisterAssembly(TestAssembly, 7, false);
        a.RegisterCommandInstance(new AssemblyOrderCommands(_records.Enqueue));
        a.ScanType(typeof(AssemblyOrderCommands));

        await SendAsync(manager, "order");
        Assert.Empty(DrainRecords());

        CommandAssemblyState state = Assert.Single(manager.GetAssemblyStates());
        Assert.Equal(7, state.Priority);
        Assert.False(state.IsEnabled);
        Assert.False(Assert.Single(state.Types).IsEffectivelyEnabled);

        Assert.True(a.SetEnabled(true));
        await SendAsync(manager, "order");
        Assert.Equal(["A2", "A1"], DrainRecords());
    }

    /// <see cref="CommandManager.GetAssemblyStates" />
    [Fact]
    public void GetAssemblyStates_ReportsOrderAndEffectiveStates()
    {
        CommandManager       manager = new();
        AssemblyRegistration b       = RegisterB(manager, 5);
        AssemblyRegistration a       = RegisterA(manager, 10);
        AddRecordingCommand(a, typeof(SecondOrderCommands), "Dyn", 0);
        Assert.True(b.SetEnabled(false));
        Assert.True(a.SetTypeEnabled(typeof(SecondOrderCommands), false));

        IReadOnlyList<CommandAssemblyState> states = manager.GetAssemblyStates();

        Assert.Equal([TestAssembly, FixtureAssembly], states.Select(state => state.Assembly));
        Assert.Equal(
            [(typeof(AssemblyOrderCommands), true, true, 2), (typeof(SecondOrderCommands), false, false, 1)],
            states[0].Types.Select(type => (type.Type, type.IsEnabled, type.IsEffectivelyEnabled, type.CommandCount)));
        CommandTypeState fixtureType = Assert.Single(states[1].Types);
        Assert.True(fixtureType.IsEnabled);
        Assert.False(fixtureType.IsEffectivelyEnabled);
    }

#endregion

#region Unregistration Tests

    /// <see cref="AssemblyRegistration.UnregisterAsync" />
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unregister_WaitsForInFlightInvocation(bool preventReentry)
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = manager.RegisterAssembly(TestAssembly);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate    = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AddGatedCommand(a, "Gate", started, gate, preventReentry);

        Task running = SendAsync(manager, "order").AsTask();
        await started.Task.WaitAsync(Timeout, CT);
        Task unregister = a.UnregisterAsync(CT).AsTask();

        Assert.False(unregister.IsCompleted);
        Assert.False(a.IsRegistered);
        Assert.False(manager.TryGetAssembly(TestAssembly, out _));

        gate.TrySetResult();
        await running.WaitAsync(Timeout, CT);
        await unregister.WaitAsync(Timeout, CT);
    }

    /// <see cref="AssemblyRegistration.UnregisterAsync" />
    [Fact]
    public async Task Unregister_SkipsRemainingCommandsOfInFlightMessage()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 10);
        RegisterB(manager, 5);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate    = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AddGatedCommand(a, "Gate", started, gate);

        Task running = SendAsync(manager, "order").AsTask();
        await started.Task.WaitAsync(Timeout, CT);
        Task unregister = a.UnregisterAsync(CT).AsTask();
        gate.TrySetResult();
        await running.WaitAsync(Timeout, CT);
        await unregister.WaitAsync(Timeout, CT);

        Assert.Equal(["Gate", "B1"], _records);
    }

    /// <see cref="AssemblyRegistration.UnregisterAsync" />
    [Fact]
    public async Task Unregister_CancellationStopsOnlyWaiting()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = manager.RegisterAssembly(TestAssembly);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate    = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AddGatedCommand(a, "Gate", started, gate);

        Task running = SendAsync(manager, "order").AsTask();
        await started.Task.WaitAsync(Timeout, CT);
        using CancellationTokenSource cts        = new();
        Task                          unregister = a.UnregisterAsync(cts.Token).AsTask();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unregister);
        Assert.False(a.IsRegistered);
        Assert.False(manager.TryGetAssembly(TestAssembly, out _));

        gate.TrySetResult();
        await running.WaitAsync(Timeout, CT);
        await a.UnregisterAsync(CT).AsTask().WaitAsync(Timeout, CT);
    }

    /// <see cref="AssemblyRegistration.UnregisterAsync" />
    [Fact]
    public async Task InvocationExitPaths_ReleaseInFlightCount()
    {
        // Ordinary handler exception.
        CommandManager       throwingManager = new();
        AssemblyRegistration throwing        = throwingManager.RegisterAssembly(TestAssembly);
        throwing.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ => throw new InvalidOperationException("handler failure"),
            ["exit"]);
        await SendAsync(throwingManager, "exit");
        AssertDrainedImmediately(throwing);

        // Before-filter short circuit.
        CommandManager       blockedManager = new();
        AssemblyRegistration blocked        = blockedManager.RegisterAssembly(TestAssembly);
        blocked.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ => ValueTask.CompletedTask,
            ["exit"],
            beforeFilters: [new ManagementBlockingBeforeAttribute()]);
        await SendAsync(blockedManager, "exit");
        AssertDrainedImmediately(blocked);

        // Re-entry rejection while the first invocation is still running.
        CommandManager       reentryManager = new();
        AssemblyRegistration reentry        = reentryManager.RegisterAssembly(TestAssembly);
        TaskCompletionSource started        = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate           = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AddGatedCommand(reentry, "Gate", started, gate);
        Task first = SendAsync(reentryManager, "order").AsTask();
        await started.Task.WaitAsync(Timeout, CT);
        await SendAsync(reentryManager, "order");
        gate.TrySetResult();
        await first.WaitAsync(Timeout, CT);
        Assert.Equal(["Gate"], _records);
        AssertDrainedImmediately(reentry);

        // Sora-owned cancellation.
        CommandManager                               cancelManager = new();
        AssemblyRegistration                         cancel        = cancelManager.RegisterAssembly(TestAssembly);
        ManagementWaitForCancellationBeforeAttribute waiter        = new();
        cancel.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            _ => ValueTask.CompletedTask,
            ["exit"],
            beforeFilters: [waiter]);
        using CancellationTokenSource cts = new();
        Task canceled = cancelManager.HandleMessageEventAsync(CreateEvent("exit"), cts.Token).AsTask();
        await waiter.Started.Task.WaitAsync(Timeout, CT);
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        AssertDrainedImmediately(cancel);
    }

    /// <see cref="AssemblyRegistration.IsRegistered" />
    [Fact]
    public async Task StaleRegistration_RejectsRegistrationAndIgnoresSetters()
    {
        CommandManager       manager = new();
        AssemblyRegistration a       = RegisterA(manager, 4);
        Guid                 id      = AddRecordingCommand(a, typeof(AssemblyOrderCommands), "Dyn", 0);
        await a.UnregisterAsync(CT);

        Assert.False(a.SetEnabled(false));
        Assert.False(a.SetPriority(1));
        Assert.False(a.SetTypeEnabled(typeof(AssemblyOrderCommands), false));
        Assert.Throws<InvalidOperationException>(() => a.ScanType(typeof(SecondOrderCommands)));
        Assert.Throws<InvalidOperationException>(() => a.ScanCommandGroups());
        Assert.Throws<InvalidOperationException>(() => a.RegisterCommandInstance(new SecondOrderCommands(_ => { })));
        Assert.Throws<InvalidOperationException>(() => RegisterNoopCommand(a, typeof(AssemblyOrderCommands)));
        Assert.False(a.Registrar.UnregisterDynamicCommand(id));
        Assert.True(a.UnregisterAsync(CT).IsCompletedSuccessfully);
        Assert.Null(manager.GetCommandInstance<AssemblyOrderCommands>());

        await SendAsync(manager, "order");
        Assert.Empty(_records);

        AssemblyRegistration renewed = RegisterA(manager, 0);
        Assert.NotSame(a, renewed);
        Assert.True(renewed.IsRegistered);
        await SendAsync(manager, "order");
        Assert.Equal(["A2", "A1"], _records);
    }

    /// <see cref="AssemblyRegistration.UnregisterAsync" />
    [Fact]
    public async Task Unregister_ReleasesCollectibleAssembly()
    {
        CommandManager manager = new();
        WeakReference  context = await RegisterAndUnregisterCollectibleFixtureAsync(manager);

        for (int attempt = 0; attempt < 20 && context.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(context.IsAlive);
    }

#endregion

#region Helpers

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> RegisterAndUnregisterCollectibleFixtureAsync(CommandManager manager)
    {
        AssemblyLoadContext  context  = new("command-fixture", true);
        Assembly             assembly = context.LoadFromAssemblyPath(FixtureAssembly.Location);
        AssemblyRegistration fixture  = manager.RegisterAssembly(assembly);
        Assert.Equal(2, fixture.ScanCommandGroups());
        await manager.HandleMessageEventAsync(CreateEvent("fixture-static"), CT);

        await fixture.UnregisterAsync(CT);
        context.Unload();
        return new WeakReference(context);
    }

    private AssemblyRegistration RegisterA(CommandManager manager, int priority)
    {
        AssemblyRegistration registration = manager.RegisterAssembly(TestAssembly, priority);
        registration.RegisterCommandInstance(new AssemblyOrderCommands(_records.Enqueue));
        registration.ScanType(typeof(AssemblyOrderCommands));
        return registration;
    }

    private AssemblyRegistration RegisterB(CommandManager manager, int priority)
    {
        AssemblyRegistration registration = manager.RegisterAssembly(FixtureAssembly, priority);
        registration.RegisterCommandInstance(new FixtureOrderCommands(_records.Enqueue));
        registration.ScanType(typeof(FixtureOrderCommands));
        return registration;
    }

    private void RegisterSecondType(AssemblyRegistration registration)
    {
        registration.RegisterCommandInstance(new SecondOrderCommands(_records.Enqueue));
        registration.ScanType(typeof(SecondOrderCommands));
    }

    private Guid AddRecordingCommand(AssemblyRegistration registration, Type ownerType, string name, int priority) =>
        registration.Registrar.RegisterDynamicCommand(
            ownerType,
            _ =>
            {
                _records.Enqueue(name);
                return ValueTask.CompletedTask;
            },
            ["order"],
            MatchType.Keyword,
            priority: priority,
            blockAfterMatch: false);

    private void AddGatedCommand(
        AssemblyRegistration registration,
        string               name,
        TaskCompletionSource started,
        TaskCompletionSource gate,
        bool                 preventReentry = true) =>
        registration.Registrar.RegisterDynamicCommand(
            typeof(AssemblyOrderCommands),
            async _ =>
            {
                started.TrySetResult();
                await gate.Task;
                _records.Enqueue(name);
            },
            ["order"],
            MatchType.Keyword,
            priority: 1000,
            blockAfterMatch: false,
            preventReentry: preventReentry);

    private static Guid RegisterNoopCommand(AssemblyRegistration registration, Type ownerType) =>
        registration.Registrar.RegisterDynamicCommand(ownerType, _ => ValueTask.CompletedTask, ["noop"]);

    private static void AssertDrainedImmediately(AssemblyRegistration registration) =>
        Assert.True(registration.UnregisterAsync(CT).IsCompletedSuccessfully);

    private string[] DrainRecords()
    {
        List<string> records = [];
        while (_records.TryDequeue(out string? record)) records.Add(record);
        return [.. records];
    }

    private static ValueTask SendAsync(CommandManager manager, string text) =>
        manager.HandleMessageEventAsync(CreateEvent(text), CT);

    private static MessageReceivedEvent CreateEvent(string text) =>
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