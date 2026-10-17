using System.Reflection;
using Sora.Command.InternalEntities;

namespace Sora.Command;

/// <summary>
///     One registration lifetime of an assembly inside a <see cref="CommandManager" />.
///     Every scanned or dynamic command of the assembly is registered through this handle, and the assembly's enabled
///     state, priority, type-level enabled states and unregistration are managed here.
///     Obtain it from <see cref="CommandManager.RegisterAssembly" />; registering the same assembly again after
///     <see cref="UnregisterAsync" /> creates a new handle.
/// </summary>
public sealed class AssemblyRegistration
{
#region Fields

    private readonly Lock                  _invocationLock = new();
    private          TaskCompletionSource? _drainSource;
    private          int                   _inFlightCount;
    private volatile bool                  _isRegistered = true;

#endregion

#region Constructor

    /// <summary>
    ///     Creates an assembly registration. Only <see cref="CommandManager" /> creates registrations.
    /// </summary>
    /// <param name="manager">The owning manager.</param>
    /// <param name="assembly">The registered assembly.</param>
    /// <param name="priority">The initial assembly priority.</param>
    /// <param name="enabled">The initial assembly enabled state.</param>
    /// <param name="firstSequence">The manager-wide registration sequence of the assembly.</param>
    internal AssemblyRegistration(
        CommandManager manager,
        Assembly       assembly,
        int            priority,
        bool           enabled,
        long           firstSequence)
    {
        Manager       = manager;
        Assembly      = assembly;
        Priority      = priority;
        IsEnabled     = enabled;
        FirstSequence = firstSequence;
        Registrar     = new CommandRegistrar(this);
    }

#endregion

#region Properties

    /// <summary>The registered assembly.</summary>
    public Assembly Assembly { get; }

    /// <summary>
    ///     Whether this handle is still registered. It becomes <c>false</c> as soon as <see cref="UnregisterAsync" />
    ///     starts, before in-flight invocations finish.
    /// </summary>
    public bool IsRegistered => _isRegistered;

    /// <summary>The assembly priority. Higher priority assemblies execute first.</summary>
    public int Priority { get; internal set; }

    /// <summary>The assembly's local enabled state.</summary>
    public bool IsEnabled { get; internal set; }

    /// <summary>
    ///     A dynamic-command registrar bound to this registration. Hosts can pass it to plugins without exposing
    ///     management operations.
    /// </summary>
    public ICommandRegistrar Registrar { get; }

    /// <summary>The owning manager.</summary>
    internal CommandManager Manager { get; }

    /// <summary>Manager-wide registration sequence of the assembly; orders assemblies with equal priority.</summary>
    internal long FirstSequence { get; }

    /// <summary>All registered commands of the assembly, including disabled types.</summary>
    internal List<CommandInfo> Commands { get; } = [];

    /// <summary>Type-level states keyed by owner type.</summary>
    internal Dictionary<Type, TypeState> Types { get; } = new();

    /// <summary>Singleton instances for instance command handlers, keyed by scanned type.</summary>
    internal Dictionary<Type, object> Instances { get; } = new();

    /// <summary>Types that have completed scanning in this registration.</summary>
    internal HashSet<Type> ScannedTypes { get; } = [];

    /// <summary>The cached execution segment, or null when no command is effectively enabled.</summary>
    internal AssemblySegment? Segment { get; set; }

#endregion

#region Registration

    /// <summary>
    ///     Pre-registers a singleton instance for a command group type of this assembly.
    ///     Use this to provide externally constructed instances (e.g., from a DI container).
    ///     Must be called before the type is scanned.
    ///     Recommended for every type with instance commands: without a pre-registered instance, a parameterless
    ///     constructor is used, and if none exists the instance is created by
    ///     <see cref="System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject" /> without running any
    ///     constructor or field initializer.
    /// </summary>
    /// <remarks>
    ///     The instance is keyed by <typeparamref name="T" />, the static type of the argument, not its runtime type.
    ///     Pass the concrete command type; passing a base-class variable registers the instance under the base type.
    /// </remarks>
    /// <typeparam name="T">The command group type. It must be defined in <see cref="Assembly" />.</typeparam>
    /// <param name="instance">The instance to use for command invocation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance" /> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T" /> is not defined in <see cref="Assembly" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     The type has already been scanned, or this registration has been unregistered.
    /// </exception>
    public void RegisterCommandInstance<T>(T instance) where T : class =>
        Manager.RegisterCommandInstance(this, typeof(T), instance);

    /// <summary>
    ///     Scans the assembly's exported classes with <see cref="CommandGroupAttribute" /> for command methods (both
    ///     static and instance). Abstract non-static classes and open generic types are skipped. Types already scanned
    ///     in this registration are skipped with a warning. All new commands of the call are published together; if
    ///     scanning throws, nothing from the call is published.
    /// </summary>
    /// <returns>The number of newly registered commands.</returns>
    /// <exception cref="InvalidOperationException">This registration has been unregistered.</exception>
    public int ScanCommandGroups() => Manager.ScanCommandGroups(this);

    /// <summary>
    ///     Scans a single type of this assembly for command methods (both static and instance). The type does not need
    ///     <see cref="CommandGroupAttribute" />. Inherited command methods belong to the scanned type. Scanning a type
    ///     already scanned in this registration returns 0 with a warning.
    /// </summary>
    /// <param name="type">The type to scan.</param>
    /// <returns>The number of newly registered commands.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="type" /> is not defined in <see cref="Assembly" />, or is an abstract non-static class or an
    ///     open generic type.
    /// </exception>
    /// <exception cref="InvalidOperationException">This registration has been unregistered.</exception>
    public int ScanType(Type type) => Manager.ScanType(this, type);

#endregion

#region Management

    /// <summary>
    ///     Sets the assembly's local enabled state. Disabling keeps all registrations, instances and filter state.
    ///     Re-enabling restores each type's own local state. Takes effect from the next message.
    /// </summary>
    /// <param name="enabled">The new local enabled state.</param>
    /// <returns><c>true</c> if this registration is still registered; otherwise <c>false</c>.</returns>
    public bool SetEnabled(bool enabled) => Manager.SetAssemblyEnabled(this, enabled);

    /// <summary>
    ///     Sets the assembly priority. Higher priority assemblies execute first; equal priorities keep the assembly
    ///     registration order. Takes effect from the next message.
    /// </summary>
    /// <param name="priority">The new priority.</param>
    /// <returns><c>true</c> if this registration is still registered; otherwise <c>false</c>.</returns>
    public bool SetPriority(int priority) => Manager.SetAssemblyPriority(this, priority);

    /// <summary>
    ///     Sets the local enabled state of an owner type in this assembly. A type's commands match only when both the
    ///     assembly and the type are enabled. Takes effect from the next message.
    /// </summary>
    /// <param name="type">An owner type of a scanned or dynamic command in this registration.</param>
    /// <param name="enabled">The new local enabled state.</param>
    /// <returns>
    ///     <c>true</c> if the type owns commands in this registration; <c>false</c> for unknown or foreign types or when
    ///     this registration has been unregistered.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is null.</exception>
    public bool SetTypeEnabled(Type type, bool enabled) => Manager.SetTypeEnabled(this, type, enabled);

    /// <summary>
    ///     Unregisters the assembly. Before the first await, all commands, type states, scan records and instance
    ///     references of this registration are removed, so no new invocation starts and messages already in progress
    ///     skip the assembly's remaining commands. The returned task then completes when in-flight invocations
    ///     (before-filters, handler including follow-up waits, after-filters) have finished. Instances are not disposed.
    ///     Calling it again waits for the same drain.
    /// </summary>
    /// <param name="ct">Cancels only the wait; the unregistration itself is not rolled back.</param>
    /// <returns>A task that completes when the registration has drained.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="ct" /> was canceled before draining finished.</exception>
    public ValueTask UnregisterAsync(CancellationToken ct = default) => Manager.UnregisterAssemblyAsync(this, ct);

#endregion

#region In-flight Tracking

    /// <summary>
    ///     Enters an in-flight invocation if the registration is still registered.
    /// </summary>
    /// <returns><c>true</c> if entered; <c>false</c> if the registration has been unregistered.</returns>
    internal bool TryEnterInvocation()
    {
        lock (_invocationLock)
        {
            if (!_isRegistered) return false;
            _inFlightCount++;
            return true;
        }
    }

    /// <summary>
    ///     Exits an in-flight invocation and completes the drain when the last invocation of an unregistered
    ///     registration ends.
    /// </summary>
    internal void ExitInvocation()
    {
        TaskCompletionSource? drained = null;
        lock (_invocationLock)
        {
            _inFlightCount--;
            if (_inFlightCount == 0 && !_isRegistered) drained = _drainSource;
        }

        drained?.TrySetResult();
    }

    /// <summary>
    ///     Marks the registration as unregistered.
    /// </summary>
    /// <returns>A task that completes when no in-flight invocation remains.</returns>
    internal Task MarkUnregistered()
    {
        lock (_invocationLock)
        {
            if (_drainSource is not null) return _drainSource.Task;

            _isRegistered = false;
            _drainSource  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_inFlightCount == 0) _drainSource.TrySetResult();
            return _drainSource.Task;
        }
    }

#endregion
}
