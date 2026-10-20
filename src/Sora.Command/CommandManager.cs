using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Sora.Command.InternalEntities;

namespace Sora.Command;

/// <summary>
///     Registers command assemblies and executes their commands based on message content.
///     Commands are registered only through an <see cref="AssemblyRegistration" /> returned by
///     <see cref="RegisterAssembly" />. Assemblies execute in descending assembly priority (equal priorities keep
///     registration order); commands inside an assembly execute in descending command priority (equal priorities keep
///     registration order). All commands of one assembly finish before the next assembly starts.
/// </summary>
public sealed class CommandManager
{
#region Fields

    private readonly ILogger                                    _logger = SoraLogger.CreateLogger<CommandManager>();
    private readonly Dictionary<MatchType, ICommandMatcher>     _matchers = new();
    private readonly Dictionary<Assembly, AssemblyRegistration> _registrations = new();
    private readonly Lock                                       _registrationLock = new();
    private          ExecutionTable                             _executionTable = ExecutionTable.Empty;
    private          long                                       _nextSequence;

    /// <summary>
    ///     Tracks in-flight command executions for re-entry protection.
    ///     Key: (Method, ConnectionId, SenderId, GroupId, SourceType).
    /// </summary>
    private readonly ConcurrentDictionary<ExecutionKey, byte> _activeExecutions = new();

#endregion

#region Constructor

    /// <summary>
    ///     Creates a new CommandManager and registers default matchers.
    /// </summary>
    public CommandManager()
    {
        _matchers[MatchType.Full]    = new FullMatcher();
        _matchers[MatchType.Regex]   = new RegexMatcher();
        _matchers[MatchType.Keyword] = new KeywordMatcher();
    }

#endregion

#region Assembly Registration

    /// <summary>
    ///     Registers an assembly and returns its registration handle. The priority and enabled state are initial values
    ///     applied only when the assembly is newly registered; if it is already registered, the existing handle is
    ///     returned, the arguments are ignored and a warning is logged.
    ///     Registering does not scan; call <see cref="AssemblyRegistration.ScanCommandGroups" /> or
    ///     <see cref="AssemblyRegistration.ScanType" /> afterwards.
    /// </summary>
    /// <param name="assembly">The assembly to register. Assemblies are identified by runtime object, not by name.</param>
    /// <param name="priority">Initial assembly priority. Higher priority assemblies execute first.</param>
    /// <param name="enabled">Initial assembly enabled state.</param>
    /// <returns>The assembly registration handle.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
    public AssemblyRegistration RegisterAssembly(Assembly assembly, int priority = 0, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        lock (_registrationLock)
        {
            if (_registrations.TryGetValue(assembly, out AssemblyRegistration? existing))
            {
                _logger.LogWarning(
                    "Command assembly {AssemblyName} is already registered; returning the existing registration and ignoring priority {Priority} and enabled {Enabled}",
                    assembly.GetName().Name,
                    priority,
                    enabled);
                return existing;
            }

            AssemblyRegistration registration = new(this, assembly, priority, enabled, _nextSequence++);
            _registrations.Add(assembly, registration);
            PublishTable();

            _logger.LogInformation(
                "Registered command assembly {AssemblyName} (priority: {Priority}, enabled: {Enabled})",
                assembly.GetName().Name,
                priority,
                enabled);
            return registration;
        }
    }

    /// <summary>
    ///     Gets the current registration of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to look up.</param>
    /// <param name="registration">The registration if found.</param>
    /// <returns><c>true</c> if the assembly is registered; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
    public bool TryGetAssembly(Assembly assembly, [NotNullWhen(true)] out AssemblyRegistration? registration)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        lock (_registrationLock)
        {
            return _registrations.TryGetValue(assembly, out registration);
        }
    }

    /// <summary>
    ///     Returns an immutable snapshot of all registered assemblies in execution order (descending priority, then
    ///     registration order), including disabled assemblies.
    /// </summary>
    /// <returns>The assembly states.</returns>
    public IReadOnlyList<CommandAssemblyState> GetAssemblyStates()
    {
        lock (_registrationLock)
        {
            return [.. OrderAssemblies(_registrations.Values).Select(CreateAssemblyState)];
        }
    }

    /// <summary>
    ///     Gets the singleton instance used for command handlers of the specified type.
    ///     Returns null if the type's assembly is not registered or the type has no instance.
    /// </summary>
    /// <typeparam name="T">The command group type.</typeparam>
    /// <returns>The singleton instance, or null if not registered.</returns>
    public T? GetCommandInstance<T>() where T : class
    {
        lock (_registrationLock)
        {
            return _registrations.TryGetValue(typeof(T).Assembly, out AssemblyRegistration? registration)
                   && registration.Instances.TryGetValue(typeof(T), out object? instance)
                ? (T)instance
                : null;
        }
    }

#endregion

#region Command Registration

    /// <summary>
    ///     Pre-registers a singleton instance for a type of the registration's assembly.
    /// </summary>
    internal void RegisterCommandInstance(AssemblyRegistration registration, Type type, object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_registrationLock)
        {
            EnsureRegistered(registration);
            EnsureOwnedType(registration, type, nameof(instance));
            if (registration.ScannedTypes.Contains(type))
                throw new InvalidOperationException(
                    $"RegisterCommandInstance<{type.Name}>() must be called before {type.Name} is scanned.");

            registration.Instances[type] = instance;
        }
    }

    /// <summary>
    ///     Registers a dynamic command owned by a type of the registration's assembly.
    /// </summary>
    internal Guid RegisterDynamicCommand(
        AssemblyRegistration                  registration,
        Type                                  ownerType,
        Func<MessageReceivedEvent, ValueTask> handler,
        string[]                              expressions,
        MatchType                             matchType,
        MessageSourceScope                    sourceScope,
        MemberRole                            permissionLevel,
        int                                   priority,
        bool                                  blockAfterMatch,
        string                                description,
        bool                                  preventReentry,
        string                                reentryMessage,
        string                                prefix,
        CommandBeforeFilterAttribute[]?       beforeFilters,
        CommandAfterFilterAttribute[]?        afterFilters,
        bool                                  superUserOnly)
    {
        ArgumentNullException.ThrowIfNull(ownerType);
        ArgumentNullException.ThrowIfNull(handler);

        Guid commandId = Guid.NewGuid();

        // Auto-discover filter attributes applied directly to the lambda / method group.
        Attribute[] handlerAttrs = [.. handler.Method.GetCustomAttributes(true).OfType<Attribute>()];
        IEnumerable<CommandBeforeFilterAttribute> handlerBefore = handlerAttrs.OfType<CommandBeforeFilterAttribute>();
        IEnumerable<CommandAfterFilterAttribute> handlerAfter = handlerAttrs.OfType<CommandAfterFilterAttribute>();

        // Merge auto-discovered + explicit, dedup by reference (guards against the user passing the
        // same instance twice), then sort stably by Order. Reference dedup preserves the intentional
        // case of "two distinct instances of the same filter type" (e.g., two different Cooldown values).
        CommandBeforeFilterAttribute[] frozenBefore =
            [.. DistinctByReference(handlerBefore.Concat(beforeFilters ?? [])).OrderBy(static f => f.Order)];
        CommandAfterFilterAttribute[] frozenAfter =
            [.. DistinctByReference(handlerAfter.Concat(afterFilters ?? [])).OrderBy(static f => f.Order)];

        // Pin all attributes (handler-method attrs + explicitly-passed filter instances) onto
        // CommandInfo.Attributes so CommandFilterContext.Attributes can observe identical references.
        // Reference dedup prevents duplicate entries when the same instance is reachable via multiple
        // sources (e.g., user passes the same filter twice in beforeFilters).
        IReadOnlyList<Attribute> attributes =
            [.. DistinctByReference(handlerAttrs.Concat(beforeFilters ?? []).Concat(afterFilters ?? []))];

        lock (_registrationLock)
        {
            EnsureRegistered(registration);
            EnsureOwnedType(registration, ownerType, nameof(ownerType));

            CommandInfo info = new()
            {
                CommandId       = commandId,
                Method          = handler.Method,
                DynamicHandler  = handler,
                Expressions     = expressions,
                MatchType       = matchType,
                SourceScope     = sourceScope,
                PermissionLevel = permissionLevel,
                SuperUserOnly   = superUserOnly,
                Priority        = priority,
                BlockAfterMatch = blockAfterMatch,
                Description     = description,
                PreventReentry  = preventReentry,
                ReentryMessage  = reentryMessage,
                CommandPrefix   = prefix,
                BeforeFilters   = frozenBefore,
                AfterFilters    = frozenAfter,
                Attributes      = attributes,
                Registration    = registration,
                OwnerType       = ownerType,
                Sequence        = _nextSequence++
            };

            AddCommand(registration, info);
            RebuildSegment(registration);
            PublishTable();
        }

        _logger.LogInformation(
            "Registered dynamic command [{CommandName}] via {MatchType} (id: {CommandId}, owner: {OwnerType}, scope: {SourceScope}, priority: {Priority}, block: {BlockAfterMatch}, before-filters: {BeforeCount}, after-filters: {AfterCount})",
            handler.Method.Name,
            matchType,
            commandId,
            ownerType.FullName,
            sourceScope,
            priority,
            blockAfterMatch,
            frozenBefore.Length,
            frozenAfter.Length);
        WarnIfEmptySourceScope(handler.Method.Name, sourceScope);

        return commandId;
    }

    /// <summary>
    ///     Unregisters a dynamic command of the registration by its ID.
    /// </summary>
    internal bool UnregisterDynamicCommand(AssemblyRegistration registration, Guid commandId)
    {
        lock (_registrationLock)
        {
            if (!registration.IsRegistered) return false;

            int index = registration.Commands.FindIndex(c => c.DynamicHandler is not null && c.CommandId == commandId);
            if (index < 0) return false;

            registration.Commands.RemoveAt(index);
            RebuildSegment(registration);
            PublishTable();
        }

        _logger.LogInformation("Unregistered dynamic command (id: {CommandId})", commandId);
        return true;
    }

    /// <summary>
    ///     Scans the registration's exported command group classes and publishes all new commands together.
    /// </summary>
    internal int ScanCommandGroups(AssemblyRegistration registration)
    {
        lock (_registrationLock)
        {
            EnsureRegistered(registration);

            List<CommandInfo>        batch        = [];
            Dictionary<Type, object> newInstances = new();
            List<Type>               scannedTypes = [];
            foreach (Type type in registration.Assembly.GetExportedTypes()
                                              .Where(type => type.IsDefined(typeof(CommandGroupAttribute), false)
                                                             && type.IsClass))
            {
                if (!IsScannable(type))
                {
                    _logger.LogDebug(
                        "Skipped command group [{TypeName}]: abstract or open generic types cannot be instantiated",
                        type.FullName);
                    continue;
                }

                if (registration.ScannedTypes.Contains(type))
                {
                    LogDuplicateScan(registration, type);
                    continue;
                }

                BuildTypeCommands(registration, type, batch, newInstances);
                scannedTypes.Add(type);
            }

            PublishScan(registration, batch, newInstances, scannedTypes);
            _logger.LogInformation(
                "Scanned {Count} commands from assembly {AssemblyName}",
                batch.Count,
                registration.Assembly.GetName().Name);
            return batch.Count;
        }
    }

    /// <summary>
    ///     Scans a single type of the registration's assembly and publishes its commands together.
    /// </summary>
    internal int ScanType(AssemblyRegistration registration, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_registrationLock)
        {
            EnsureRegistered(registration);
            EnsureOwnedType(registration, type, nameof(type));
            if (!IsScannable(type))
                throw new ArgumentException(
                    $"Type {type.FullName} is abstract or an open generic type and cannot be scanned.",
                    nameof(type));

            if (registration.ScannedTypes.Contains(type))
            {
                LogDuplicateScan(registration, type);
                return 0;
            }

            List<CommandInfo>        batch        = [];
            Dictionary<Type, object> newInstances = new();
            BuildTypeCommands(registration, type, batch, newInstances);
            PublishScan(registration, batch, newInstances, [type]);
            return batch.Count;
        }
    }

    /// <summary>
    ///     Builds the commands of one type into a pending batch. Nothing is published until the whole scan succeeds.
    /// </summary>
    private void BuildTypeCommands(
        AssemblyRegistration     registration,
        Type                     type,
        List<CommandInfo>        batch,
        Dictionary<Type, object> newInstances)
    {
        int                    commandCount = 0;
        CommandGroupAttribute? groupAttr    = type.GetCustomAttribute<CommandGroupAttribute>();
        string                 prefix       = groupAttr?.Prefix ?? "";

        _logger.LogDebug("Scanning command type [{TypeName}] with prefix '{Prefix}'", type.FullName, prefix);

        // Read class-level filter attributes ONCE so all commands in this group share the same instances
        // (group-level stateful filters keep shared state, e.g., a per-group counter).
        Attribute[]                    classAttrs        = [.. type.GetCustomAttributes(true).OfType<Attribute>()];
        CommandBeforeFilterAttribute[] classBeforeFilter = [.. classAttrs.OfType<CommandBeforeFilterAttribute>()];
        CommandAfterFilterAttribute[]  classAfterFilter  = [.. classAttrs.OfType<CommandAfterFilterAttribute>()];

        foreach (MethodInfo method in type.GetMethods(
                     BindingFlags.Static
                     | BindingFlags.Instance
                     | BindingFlags.Public
                     | BindingFlags.NonPublic))
        {
            CommandAttribute? cmdAttr = method.GetCustomAttribute<CommandAttribute>();
            if (cmdAttr is null) continue;

            // Validate method signature: must accept a single parameter inheriting from BotEvent and return ValueTask or Task
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || !typeof(BotEvent).IsAssignableFrom(parameters[0].ParameterType))
                continue;
            if (method.ReturnType != typeof(ValueTask) && method.ReturnType != typeof(Task))
                continue;

            // For instance methods, get or create the singleton instance
            object? instance = method.IsStatic ? null : ResolveInstance(registration, type, newInstances);

            CommandFilterInfo filters =
                ResolveCommandFilters(method, classAttrs, classBeforeFilter, classAfterFilter);

            batch.Add(
                new CommandInfo
                {
                    Method          = method,
                    Instance        = instance,
                    Expressions     = cmdAttr.Expressions,
                    MatchType       = cmdAttr.MatchType,
                    SourceScope     = cmdAttr.SourceScope,
                    PermissionLevel = cmdAttr.PermissionLevel,
                    SuperUserOnly   = cmdAttr.SuperUserOnly,
                    Priority        = cmdAttr.Priority,
                    BlockAfterMatch = cmdAttr.BlockAfterMatch,
                    CommandPrefix   = prefix,
                    Description     = cmdAttr.Description,
                    PreventReentry  = cmdAttr.PreventReentry,
                    ReentryMessage  = cmdAttr.ReentryMessage,
                    BeforeFilters   = filters.Before,
                    AfterFilters    = filters.After,
                    Attributes      = filters.Attributes,
                    Registration    = registration,
                    OwnerType       = type,
                    Sequence        = _nextSequence++
                });

            commandCount++;
        }

        _logger.LogDebug("Built {Count} commands from type {TypeName}", commandCount, type.FullName);
    }

    /// <summary>
    ///     Publishes a successfully built scan batch: instances, scan records, type states and commands.
    /// </summary>
    private void PublishScan(
        AssemblyRegistration     registration,
        List<CommandInfo>        batch,
        Dictionary<Type, object> newInstances,
        IEnumerable<Type>        scannedTypes)
    {
        foreach (KeyValuePair<Type, object> pair in newInstances)
            registration.Instances[pair.Key] = pair.Value;
        registration.ScannedTypes.UnionWith(scannedTypes);
        foreach (CommandInfo info in batch)
            AddCommand(registration, info);

        if (batch.Count > 0)
        {
            RebuildSegment(registration);
            PublishTable();
        }

        foreach (CommandInfo info in batch)
        {
            _logger.LogInformation(
                "Registered command [{CommandName}] via {MatchType} (owner: {OwnerType}, scope: {SourceScope}, priority: {Priority}, block: {BlockAfterMatch}, before-filters: {BeforeCount}, after-filters: {AfterCount})",
                info.Method.Name,
                info.MatchType,
                info.OwnerType.FullName,
                info.SourceScope,
                info.Priority,
                info.BlockAfterMatch,
                info.BeforeFilters.Count,
                info.AfterFilters.Count);
            WarnIfEmptySourceScope(info.Method.Name, info.SourceScope);
        }
    }

    /// <summary>
    ///     Warns that a command whose source scope contains no message source can never match.
    /// </summary>
    private void WarnIfEmptySourceScope(string commandName, MessageSourceScope sourceScope)
    {
        if ((sourceScope & MessageSourceScope.Any) == 0)
            _logger.LogWarning(
                "Command [{CommandName}] declares source scope {SourceScope} without any message source; it will never match",
                commandName,
                sourceScope);
    }

    /// <summary>
    ///     Adds a command and creates its owner type state on first use.
    /// </summary>
    private static void AddCommand(AssemblyRegistration registration, CommandInfo info)
    {
        registration.Commands.Add(info);
        if (!registration.Types.ContainsKey(info.OwnerType))
            registration.Types.Add(info.OwnerType, new TypeState { FirstSequence = info.Sequence });
    }

    /// <summary>
    ///     Resolves the singleton for an instance command: pre-registered, already created in this batch, or new.
    /// </summary>
    private object ResolveInstance(
        AssemblyRegistration     registration,
        Type                     type,
        Dictionary<Type, object> newInstances)
    {
        if (registration.Instances.TryGetValue(type, out object? registered)) return registered;
        if (newInstances.TryGetValue(type, out object? created)) return created;

        object instance = CreateInstance(type);
        newInstances.Add(type, instance);
        return instance;
    }

    /// <summary>
    ///     Resolves the per-command filter arrays for a method, combining cached class-level attributes
    ///     with method-level attributes. Order: class-level first (then method-level) within the same
    ///     <see cref="CommandBeforeFilterAttribute.Order" /> tier (stable sort).
    ///     Reference-equality dedup guards against pathological cases where the same attribute instance
    ///     is reachable via multiple metadata walk paths.
    /// </summary>
    private static CommandFilterInfo ResolveCommandFilters(
        MethodInfo                                  method,
        IReadOnlyList<Attribute>                    classAttributes,
        IReadOnlyList<CommandBeforeFilterAttribute> classBefore,
        IReadOnlyList<CommandAfterFilterAttribute>  classAfter)
    {
        Attribute[] methodAttrs = [.. method.GetCustomAttributes(true).OfType<Attribute>()];

        CommandBeforeFilterAttribute[] methodBefore = [.. methodAttrs.OfType<CommandBeforeFilterAttribute>()];
        CommandAfterFilterAttribute[]  methodAfter  = [.. methodAttrs.OfType<CommandAfterFilterAttribute>()];

        // Stable sort preserves declaration order within an Order tier; concatenating class first ensures
        // class-level attributes precede method-level ones when their Order ties.
        // Reference dedup is defensive — distinct method/class GetCustomAttributes calls return distinct
        // instances today, but this protects against future reflection/inheritance edge cases.
        CommandBeforeFilterAttribute[] before =
        [
            .. DistinctByReference(classBefore.Concat(methodBefore))
                .OrderBy(static f => f.Order)
        ];
        CommandAfterFilterAttribute[] after =
        [
            .. DistinctByReference(classAfter.Concat(methodAfter))
                .OrderBy(static f => f.Order)
        ];

        IReadOnlyList<Attribute> allAttributes =
            [.. DistinctByReference(methodAttrs.Concat(classAttributes))];
        return new CommandFilterInfo
        {
            Attributes = allAttributes,
            Before     = before,
            After      = after
        };
    }

    /// <summary>
    ///     Returns elements from <paramref name="source" />, skipping any element whose reference has already been
    ///     seen. Used to deduplicate filter attribute / pinned attribute lists by reference (not by type or value),
    ///     which is the correct semantics for filters: distinct instances of the same type are intentional, but the
    ///     same instance referenced multiple times is a user mistake.
    /// </summary>
    private static IEnumerable<T> DistinctByReference<T>(IEnumerable<T> source) where T : class =>
        source.Distinct<T>(ReferenceEqualityComparer.Instance);

#endregion

#region Registration Management

    /// <summary>
    ///     Sets an assembly's local enabled state.
    /// </summary>
    internal bool SetAssemblyEnabled(AssemblyRegistration registration, bool enabled)
    {
        lock (_registrationLock)
        {
            if (!registration.IsRegistered) return false;
            if (registration.IsEnabled == enabled) return true;

            registration.IsEnabled = enabled;
            PublishTable();
        }

        _logger.LogInformation(
            "Command assembly {AssemblyName} enabled changed: {OldEnabled} -> {NewEnabled}",
            registration.Assembly.GetName().Name,
            !enabled,
            enabled);
        return true;
    }

    /// <summary>
    ///     Sets an assembly's priority.
    /// </summary>
    internal bool SetAssemblyPriority(AssemblyRegistration registration, int priority)
    {
        int oldPriority;
        lock (_registrationLock)
        {
            if (!registration.IsRegistered) return false;
            if (registration.Priority == priority) return true;

            oldPriority           = registration.Priority;
            registration.Priority = priority;
            PublishTable();
        }

        _logger.LogInformation(
            "Command assembly {AssemblyName} priority changed: {OldPriority} -> {NewPriority}",
            registration.Assembly.GetName().Name,
            oldPriority,
            priority);
        return true;
    }

    /// <summary>
    ///     Sets an owner type's local enabled state.
    /// </summary>
    internal bool SetTypeEnabled(AssemblyRegistration registration, Type type, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_registrationLock)
        {
            if (!registration.IsRegistered || !registration.Types.TryGetValue(type, out TypeState? state))
                return false;
            if (state.Enabled == enabled) return true;

            state.Enabled = enabled;
            RebuildSegment(registration);
            PublishTable();
        }

        _logger.LogInformation(
            "Command type {TypeName} in assembly {AssemblyName} enabled changed: {OldEnabled} -> {NewEnabled}",
            type.FullName,
            registration.Assembly.GetName().Name,
            !enabled,
            enabled);
        return true;
    }

    /// <summary>
    ///     Removes a registration immediately and waits for its in-flight invocations to finish.
    /// </summary>
    internal async ValueTask UnregisterAssemblyAsync(AssemblyRegistration registration, CancellationToken ct)
    {
        Task drained;
        lock (_registrationLock)
        {
            bool removing = registration.IsRegistered;
            drained = registration.MarkUnregistered();
            if (removing)
            {
                _registrations.Remove(registration.Assembly);
                registration.Commands.Clear();
                registration.Types.Clear();
                registration.Instances.Clear();
                registration.ScannedTypes.Clear();
                registration.Segment = null;
                PublishTable();

                _logger.LogInformation(
                    "Unregistered command assembly {AssemblyName}; waiting for in-flight invocations",
                    registration.Assembly.GetName().Name);
            }
        }

        await drained.WaitAsync(ct);
        _logger.LogDebug(
            "Command assembly {AssemblyName} has no in-flight invocations",
            registration.Assembly.GetName().Name);
    }

    /// <summary>
    ///     Rebuilds the cached segment of a registration: all commands sorted by descending priority and ascending
    ///     registration sequence, filtered to effectively enabled types. Called only when the registration's commands
    ///     or type states change.
    /// </summary>
    private static void RebuildSegment(AssemblyRegistration registration)
    {
        registration.Commands.Sort(CompareCommands);
        CommandInfo[] commands = [.. registration.Commands.Where(cmd => registration.Types[cmd.OwnerType].Enabled)];
        registration.Segment = commands.Length == 0
            ? null
            : new AssemblySegment
            {
                Registration = registration,
                Commands     = commands
            };
    }

    /// <summary>
    ///     Publishes a new execution table from the enabled registrations' cached segments.
    ///     Must be called while holding the registration lock.
    /// </summary>
    private void PublishTable()
    {
        AssemblySegment[] segments =
        [
            .. OrderAssemblies(_registrations.Values.Where(static registration => registration.IsEnabled))
               .Select(static registration => registration.Segment)
               .OfType<AssemblySegment>()
        ];
        Volatile.Write(ref _executionTable, new ExecutionTable { Segments = segments });
    }

    /// <summary>
    ///     Orders registrations by descending priority, then ascending registration sequence.
    /// </summary>
    private static IEnumerable<AssemblyRegistration> OrderAssemblies(IEnumerable<AssemblyRegistration> registrations) =>
        registrations.OrderByDescending(static registration => registration.Priority)
                     .ThenBy(static registration => registration.FirstSequence);

    /// <summary>
    ///     Orders commands by descending priority, then ascending registration sequence.
    /// </summary>
    private static int CompareCommands(CommandInfo left, CommandInfo right)
    {
        int priorityOrder = right.Priority.CompareTo(left.Priority);
        return priorityOrder != 0 ? priorityOrder : left.Sequence.CompareTo(right.Sequence);
    }

    /// <summary>
    ///     Creates an immutable state snapshot of a registration.
    /// </summary>
    private static CommandAssemblyState CreateAssemblyState(AssemblyRegistration registration) =>
        new()
        {
            Assembly  = registration.Assembly,
            IsEnabled = registration.IsEnabled,
            Priority  = registration.Priority,
            Types =
            [
                .. registration.Types
                               .OrderBy(static pair => pair.Value.FirstSequence)
                               .Select(pair => new CommandTypeState
                               {
                                   Type                 = pair.Key,
                                   IsEnabled            = pair.Value.Enabled,
                                   IsEffectivelyEnabled = registration.IsEnabled && pair.Value.Enabled,
                                   CommandCount         = registration.Commands.Count(cmd => cmd.OwnerType == pair.Key)
                               })
            ]
        };

    /// <summary>
    ///     Throws when a registration operation targets an unregistered handle.
    /// </summary>
    private static void EnsureRegistered(AssemblyRegistration registration)
    {
        if (!registration.IsRegistered)
            throw new InvalidOperationException(
                $"The command assembly registration for {registration.Assembly.GetName().Name} has been unregistered.");
    }

    /// <summary>
    ///     Throws when a type is not defined in the registration's assembly.
    /// </summary>
    private static void EnsureOwnedType(AssemblyRegistration registration, Type type, string paramName)
    {
        ArgumentNullException.ThrowIfNull(type, paramName);
        if (type.Assembly != registration.Assembly)
            throw new ArgumentException(
                $"Type {type.FullName} is not defined in assembly {registration.Assembly.GetName().Name}.",
                paramName);
    }

    /// <summary>
    ///     Whether a type can provide commands: abstract non-static classes and open generic types are excluded.
    /// </summary>
    private static bool IsScannable(Type type) =>
        !type.ContainsGenericParameters && (!type.IsAbstract || type.IsSealed);

    /// <summary>
    ///     Logs a skipped duplicate type scan.
    /// </summary>
    private void LogDuplicateScan(AssemblyRegistration registration, Type type) =>
        _logger.LogWarning(
            "Command type {TypeName} has already been scanned in assembly {AssemblyName}; skipping",
            type.FullName,
            registration.Assembly.GetName().Name);

#endregion

#region Command Execution

    /// <summary>
    ///     Processes a message event, matching and executing commands.
    ///     Called by the event pipeline.
    /// </summary>
    /// <param name="e">The message event to process.</param>
    /// <param name="ct">Cancellation token to interrupt command processing.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    internal async ValueTask HandleMessageEventAsync(MessageReceivedEvent e, CancellationToken ct = default)
    {
        string text = e.Message.BodySnapshot.GetText().Trim();
        if (string.IsNullOrEmpty(text)) return;

        // The table is immutable, already ordered and rebuilt only when registrations change.
        ExecutionTable table = Volatile.Read(ref _executionTable);
        foreach (AssemblySegment segment in table.Segments)
        foreach (CommandInfo cmd in segment.Commands)
        {
            ct.ThrowIfCancellationRequested();
            if (!e.IsContinueEventChain) return;
            if (!IsCommandMatched(cmd, e, text)) continue;

            _logger.LogInformation(
                "Matched command [{CommandName}] via {MatchType} ",
                cmd.Method.Name,
                cmd.MatchType);

            // An assembly unregistered after this table was read skips its remaining commands.
            if (!segment.Registration.TryEnterInvocation()) break;

            // Re-entry guard: skip if the same user already has this command in flight
            ExecutionKey executionKey = default;
            if (cmd.PreventReentry)
            {
                executionKey = new ExecutionKey(
                    cmd.Method,
                    e.ConnectionId,
                    e.Message.SenderId,
                    e.Message.GroupId,
                    e.Message.SourceType);

                if (!_activeExecutions.TryAdd(executionKey, 0))
                {
                    segment.Registration.ExitInvocation();
                    _logger.LogWarning(
                        "Command [{CommandName}] blocked: re-entry from sender {SenderId} in {SourceType} (group: {GroupId})",
                        cmd.Method.Name,
                        e.Message.SenderId,
                        e.Message.SourceType,
                        e.Message.GroupId);

                    if (!string.IsNullOrEmpty(cmd.ReentryMessage))
                        _ = SendReentryReplyAsync(e, cmd.ReentryMessage);

                    if (cmd.BlockAfterMatch) e.IsContinueEventChain = false;
                    continue;
                }
            }

            using CommandExecutionScope executionScope = new(
                segment.Registration,
                cmd.PreventReentry ? _activeExecutions : null,
                executionKey);
            await ExecuteMatchedCommandAsync(cmd, e, ct);

            if (cmd.BlockAfterMatch) e.IsContinueEventChain = false;
        }
    }

    /// <summary>
    ///     Checks source scope, permissions and match expressions of a command.
    /// </summary>
    private bool IsCommandMatched(CommandInfo cmd, MessageReceivedEvent e, string text)
    {
        // Filter by source scope
        if (!cmd.SourceScope.Contains(e.Message.SourceType))
            return false;

        // Check permission
        if (cmd.SuperUserOnly && !e.IsSuperUser)
            return false;
        if (cmd.PermissionLevel > MemberRole.Member && e.Member is not null && e.Member.Role < cmd.PermissionLevel)
            return false;

        // Try matching
        return _matchers.TryGetValue(cmd.MatchType, out ICommandMatcher? matcher)
               && cmd.Expressions.Select(expr => BuildFullExpression(cmd, expr))
                     .Any(fullExpr => matcher.IsMatch(text, fullExpr));
    }

    private async ValueTask ExecuteMatchedCommandAsync(
        CommandInfo          cmd,
        MessageReceivedEvent e,
        CancellationToken    ct)
    {
        CommandFilterContext filterContext  = CreateFilterContext(cmd);
        bool                 shortCircuited = await ExecuteBeforeFiltersAsync(cmd, e, filterContext, ct);
        Exception? commandException = shortCircuited
            ? null
            : await ExecuteCommandHandlerAsync(cmd, e, ct);
        await ExecuteAfterFiltersAsync(cmd, e, filterContext, shortCircuited, commandException, ct);
    }

    private async ValueTask<bool> ExecuteBeforeFiltersAsync(
        CommandInfo          cmd,
        MessageReceivedEvent e,
        CommandFilterContext filterContext,
        CancellationToken    ct)
    {
        foreach (CommandBeforeFilterAttribute beforeFilter in cmd.BeforeFilters)
            try
            {
                if (!await beforeFilter.OnBeforeExecuteAsync(e, filterContext, ct))
                {
                    _logger.LogDebug(
                        "Command [{CommandName}] short-circuited by {FilterType}",
                        cmd.Method.Name,
                        beforeFilter.GetType().Name);
                    return true;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException cancellation
                                       || cancellation.CancellationToken != ct
                                       || !ct.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "CommandBeforeFilter {FilterType} threw an exception, treating as pass-through",
                    beforeFilter.GetType().Name);
                if (ex is OperationCanceledException)
                    ct.ThrowIfCancellationRequested();
            }

        return false;
    }

    private async ValueTask<Exception?> ExecuteCommandHandlerAsync(
        CommandInfo          cmd,
        MessageReceivedEvent e,
        CancellationToken    ct)
    {
        try
        {
            if (cmd.DynamicHandler is not null)
            {
                await cmd.DynamicHandler(e);
            }
            else
            {
                object? result = cmd.Method.Invoke(cmd.Instance, [e]);
                switch (result)
                {
                    case ValueTask valueTask:
                        await valueTask;
                        break;
                    case Task task:
                        await task;
                        break;
                    default:
                        throw new InvalidOperationException("Command method must return Task or ValueTask");
                }
            }
        }
        catch (Exception ex)
        {
            Exception commandException = ex is TargetInvocationException { InnerException: { } innerException }
                ? innerException
                : ex;
            if (commandException is OperationCanceledException cancellation
                && cancellation.CancellationToken == ct
                && ct.IsCancellationRequested)
                ExceptionDispatchInfo.Throw(commandException);

            _logger.LogError(
                commandException,
                "Command '{CommandName}' threw an unhandled exception",
                cmd.Method.Name);
            if (commandException is OperationCanceledException)
                ct.ThrowIfCancellationRequested();
            return commandException;
        }

        return null;
    }

    private async ValueTask ExecuteAfterFiltersAsync(
        CommandInfo          cmd,
        MessageReceivedEvent e,
        CommandFilterContext filterContext,
        bool                 shortCircuited,
        Exception?           commandException,
        CancellationToken    ct)
    {
        foreach (CommandAfterFilterAttribute afterFilter in cmd.AfterFilters)
            try
            {
                await afterFilter.OnAfterExecuteAsync(e, filterContext, shortCircuited, commandException, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException cancellation
                                       || cancellation.CancellationToken != ct
                                       || !ct.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "CommandAfterFilter {FilterType} threw an exception",
                    afterFilter.GetType().Name);
                if (ex is OperationCanceledException)
                    ct.ThrowIfCancellationRequested();
            }
    }

#endregion

#region Helpers

    /// <summary>
    ///     Sends a re-entry rejection reply to the user (fire-and-forget).
    /// </summary>
    private async Task SendReentryReplyAsync(MessageReceivedEvent e, string reentryMessage)
    {
        try
        {
            MessageBody body = reentryMessage;
            switch (e.Message.SourceType)
            {
                case MessageSourceType.Group:
                    await e.Api.SendGroupMessageAsync(e.Message.GroupId, body);
                    break;
                case MessageSourceType.Friend:
                    await e.Api.SendFriendMessageAsync(e.Message.SenderId, body);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send re-entry reply for command");
        }
    }

    /// <summary>
    ///     Builds the full matching expression by combining group prefix with the command expression.
    ///     For regex match type, the prefix is escaped and inserted after the <c>^</c> anchor if present.
    /// </summary>
    /// <param name="cmd">The command info containing match type and group prefix.</param>
    /// <param name="expression">The raw command expression.</param>
    /// <returns>The combined expression ready for matching.</returns>
    private static string BuildFullExpression(CommandInfo cmd, string expression)
    {
        if (cmd.CommandPrefix.Length == 0)
            return expression;

        if (cmd.MatchType != MatchType.Regex)
            return cmd.CommandPrefix + expression;

        // For regex: escape the prefix and insert after ^ anchor if present
        string escapedPrefix = Regex.Escape(cmd.CommandPrefix);
        return expression.StartsWith('^')
            ? $"^{escapedPrefix}{expression[1..]}"
            : escapedPrefix + expression;
    }

    /// <summary>
    ///     Creates a <see cref="CommandFilterContext" /> snapshot from the internal <see cref="CommandInfo" />.
    ///     Reuses <see cref="CommandInfo.Attributes" /> directly so the context observes identical attribute
    ///     instances to the ones the framework invokes (especially important for dynamic commands where the
    ///     user-supplied <c>beforeFilters</c> / <c>afterFilters</c> instances are not reachable via reflection
    ///     on the lambda's compiled method).
    /// </summary>
    private static CommandFilterContext CreateFilterContext(CommandInfo cmd) =>
        new()
        {
            Method          = cmd.Method,
            Expressions     = Array.AsReadOnly([.. cmd.Expressions]),
            MatchType       = cmd.MatchType,
            DeclaringType   = cmd.Method.DeclaringType!,
            Attributes      = Array.AsReadOnly([.. cmd.Attributes]),
            Description     = cmd.Description,
            BlockAfterMatch = cmd.BlockAfterMatch
        };

    /// <summary>
    ///     Create instance by type
    /// </summary>
    /// <param name="type">instance type</param>
    private object CreateInstance(Type type)
    {
        // Search public and non-public parameterless constructors
        ConstructorInfo? ctor = type.GetConstructor(
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance,
            Type.EmptyTypes);
        if (ctor is not null) return ctor.Invoke(null);

        // No parameterless constructor — create uninitialized instance
        _logger.LogWarning(
            "Type {TypeName} has no parameterless constructor — using GetUninitializedObject. "
            + "Fields will not be initialized. Consider adding a parameterless constructor or "
            + "registering an instance via AssemblyRegistration.RegisterCommandInstance<T>()",
            type.FullName);
        return RuntimeHelpers.GetUninitializedObject(type);
    }

#endregion
}