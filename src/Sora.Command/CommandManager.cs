using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Sora.Command.InternalEntities;

namespace Sora.Command;

/// <summary>
///     Discovers, registers, and executes commands based on message content.
/// </summary>
public sealed class CommandManager
{
#region Fields

    private readonly List<CommandInfo> _commands = [];
    private readonly ConcurrentDictionary<Type, object> _instances = new();
    private readonly Lock _lock = new();
    private readonly Lazy<ILogger> _loggerLazy = new(SoraLogger.CreateLogger<CommandManager>);
    private          ILogger _logger => _loggerLazy.Value;
    private readonly ConcurrentDictionary<MatchType, ICommandMatcher> _matchers = new();
    private readonly HashSet<Type> _scannedTypes = [];
    private          bool _needsSort;

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
        RegisterMatcher(new FullMatcher());
        RegisterMatcher(new RegexMatcher());
        RegisterMatcher(new KeywordMatcher());
    }

#endregion

#region Instance Management

    /// <summary>
    ///     Gets the singleton instance used for command handlers of the specified type.
    ///     Returns null if the type has no registered instance commands.
    /// </summary>
    /// <typeparam name="T">The command group type.</typeparam>
    /// <returns>The singleton instance, or null if not registered.</returns>
    public T? GetCommandInstance<T>() where T : class =>
        _instances.TryGetValue(typeof(T), out object? instance) ? (T)instance : null;

    /// <summary>
    ///     Pre-registers a singleton instance for a command group type.
    ///     Use this to provide externally constructed instances (e.g., from a DI container).
    ///     Must be called before <see cref="ScanAssembly" />; throws <see cref="InvalidOperationException" /> otherwise.
    /// </summary>
    /// <typeparam name="T">The command group type.</typeparam>
    /// <param name="instance">The instance to use for command invocation.</param>
    /// <exception cref="InvalidOperationException">Thrown when called after <see cref="ScanAssembly" />.</exception>
    public void RegisterCommandInstance<T>(T instance) where T : class
    {
        if (_scannedTypes.Contains(typeof(T)))
            throw new InvalidOperationException(
                $"RegisterCommandInstance<{typeof(T).Name}>() must be called before ScanAssembly().");
        _instances[typeof(T)] = instance;
    }

#endregion

#region Command Registration

    /// <summary>
    ///     Registers a command at runtime without scanning assemblies.
    /// </summary>
    /// <param name="handler">The async handler to invoke when the command matches.</param>
    /// <param name="expressions">One or more match expressions.</param>
    /// <param name="matchType">How to match the expressions against message text.</param>
    /// <param name="sourceType">Required message source type (null = any).</param>
    /// <param name="permissionLevel">Minimum member role required.</param>
    /// <param name="priority">Higher priority commands are matched first.</param>
    /// <param name="blockAfterMatch">Whether to block the event chain after matching.</param>
    /// <param name="description">Description for help text.</param>
    /// <param name="preventReentry">
    ///     When true, prevents the same user from triggering this command while a previous invocation is still executing.
    /// </param>
    /// <param name="reentryMessage">Optional plain-text reply sent when the command is rejected due to re-entry.</param>
    /// <param name="prefix">Optional prefix prepended to match expressions (same as CommandGroup.Prefix).</param>
    /// <param name="beforeFilters">
    ///     Optional pinned before-filter attribute instances. Lambda/dynamic handlers cannot carry method-level
    ///     attributes, so callers must pass instances explicitly. Null or empty = no before-filter.
    /// </param>
    /// <param name="afterFilters">
    ///     Optional pinned after-filter attribute instances. Null or empty = no after-filter.
    ///     Same auto-discovery semantics as <paramref name="beforeFilters" />.
    /// </param>
    /// <returns>A unique command ID that can be used to unregister this command later.</returns>
    public Guid RegisterDynamicCommand(
        Func<MessageReceivedEvent, ValueTask> handler,
        string[]                              expressions,
        MatchType                             matchType       = MatchType.Full,
        MessageSourceType?                    sourceType      = null,
        MemberRole                            permissionLevel = MemberRole.Member,
        int                                   priority        = 0,
        bool                                  blockAfterMatch = true,
        string                                description     = "",
        bool                                  preventReentry  = true,
        string                                reentryMessage  = "",
        string                                prefix          = "",
        CommandBeforeFilterAttribute[]?       beforeFilters   = null,
        CommandAfterFilterAttribute[]?        afterFilters    = null)
    {
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

        CommandInfo info = new()
        {
            CommandId       = commandId,
            Method          = handler.Method,
            DynamicHandler  = handler,
            Expressions     = expressions,
            MatchType       = matchType,
            SourceType      = sourceType,
            PermissionLevel = permissionLevel,
            Priority        = priority,
            BlockAfterMatch = blockAfterMatch,
            Description     = description,
            PreventReentry  = preventReentry,
            ReentryMessage  = reentryMessage,
            CommandPrefix   = prefix,
            BeforeFilters   = frozenBefore,
            AfterFilters    = frozenAfter,
            Attributes      = attributes
        };

        lock (_lock)
        {
            _commands.Add(info);
            _needsSort = true;
        }

        _logger.LogInformation(
            "Registered dynamic command [{CommandName}] via {MatchType} (id: {CommandId}, source: {SourceType}, priority: {Priority}, block: {BlockAfterMatch}, before-filters: {BeforeCount}, after-filters: {AfterCount})",
            handler.Method.Name,
            matchType,
            commandId,
            sourceType,
            priority,
            blockAfterMatch,
            frozenBefore.Length,
            frozenAfter.Length);

        return commandId;
    }

    /// <summary>
    ///     Unregisters a previously registered dynamic command by its ID.
    /// </summary>
    /// <param name="commandId">The unique command ID returned by <see cref="RegisterDynamicCommand" />.</param>
    /// <returns><c>true</c> if the command was found and removed; <c>false</c> if no command with that ID exists.</returns>
    public bool UnregisterDynamicCommand(Guid commandId)
    {
        lock (_lock)
        {
            int index = _commands.FindIndex(c => c.CommandId == commandId);
            if (index < 0) return false;

            _commands.RemoveAt(index);
        }

        _logger.LogInformation("Unregistered dynamic command (id: {CommandId})", commandId);
        return true;
    }

    /// <summary>
    ///     Scans the given assemblies for classes with <see cref="CommandGroupAttribute" />
    ///     and methods with <see cref="CommandAttribute" />.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    public void ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies) ScanAssembly(assembly);
    }

    /// <summary>
    ///     Scans a single assembly for command methods (both static and instance).
    ///     Instance method handlers use singleton instances per declaring type.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public void ScanAssembly(Assembly assembly)
    {
        int commandCount = assembly.GetExportedTypes()
                                   .Where(type => type.IsDefined(typeof(CommandGroupAttribute), false)
                                                  && type.IsClass)
                                   .Sum(ScanType);
        _logger.LogInformation(
            "Scanned {Count} commands from assembly {AssemblyName}",
            commandCount,
            assembly.GetName().Name);
    }

    /// <summary>
    ///     Scans a single type for command methods (both static and instance).
    ///     Instance method handlers use singleton instances per declaring type.
    /// </summary>
    /// <param name="type">The type to scan for command methods.</param>
    /// <returns>The number of commands discovered and registered from the type.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="OverflowException"></exception>
    public int ScanType(Type type)
    {
        int                    commandCount = 0;
        CommandGroupAttribute? groupAttr    = type.GetCustomAttribute<CommandGroupAttribute>();
        string                 prefix       = groupAttr?.Prefix ?? "";
        _scannedTypes.Add(type);

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
            object? instance = null;
            if (!method.IsStatic)
                instance = _instances.GetOrAdd(type, CreateInstance);

            CommandFilterInfo filters =
                ResolveCommandFilters(method, classAttrs, classBeforeFilter, classAfterFilter);

            CommandInfo info = new()
            {
                Method          = method,
                Instance        = instance,
                Expressions     = cmdAttr.Expressions,
                MatchType       = cmdAttr.MatchType,
                SourceType      = cmdAttr.SourceType,
                PermissionLevel = cmdAttr.PermissionLevel,
                Priority        = cmdAttr.Priority,
                BlockAfterMatch = cmdAttr.BlockAfterMatch,
                CommandPrefix   = prefix,
                Description     = cmdAttr.Description,
                PreventReentry  = cmdAttr.PreventReentry,
                ReentryMessage  = cmdAttr.ReentryMessage,
                BeforeFilters   = filters.Before,
                AfterFilters    = filters.After,
                Attributes      = filters.Attributes
            };

            lock (_lock)
            {
                _commands.Add(info);
                _needsSort = true;
            }

            _logger.LogInformation(
                "Registered command [{CommandName}] via {MatchType} (source: {SourceType}, priority: {Priority}, block: {BlockAfterMatch}, before-filters: {BeforeCount}, after-filters: {AfterCount})",
                method.Name,
                cmdAttr.MatchType,
                cmdAttr.SourceType,
                cmdAttr.Priority,
                cmdAttr.BlockAfterMatch,
                filters.Before.Count,
                filters.After.Count);

            commandCount++;
        }

        _logger.LogInformation(
            "Scanned {Count} commands from assembly {TypeName}",
            commandCount,
            type.Name);
        return commandCount;
    }

    /// <summary>
    ///     Registers or replaces the command matcher for a match type.
    ///     Register custom matchers before registering commands that use them.
    /// </summary>
    /// <param name="matcher">The matcher to register.</param>
    private void RegisterMatcher(ICommandMatcher matcher)
    {
        _matchers[matcher.MatchType] = matcher;
        _logger.LogInformation(
            "Registered command matcher [{MatcherType}] for {MatchType}",
            matcher.GetType().Name,
            matcher.MatchType);
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
    private static IEnumerable<T> DistinctByReference<T>(IEnumerable<T> source) where T : class
    {
        // HashSet<object> happily accepts IEqualityComparer<object>; we add T (which is object) into it.
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        foreach (T item in source)
            if (seen.Add(item))
                yield return item;
    }

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
        string text = e.Message.Body.GetText().Trim();
        if (string.IsNullOrEmpty(text)) return;

        // use snapshot for thread safety
        List<CommandInfo> snapshot;
        lock (_lock)
        {
            if (_needsSort)
            {
                _commands.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _needsSort = false;
            }

            snapshot = [.. _commands];
        }

        foreach (CommandInfo cmd in snapshot)
        {
            ct.ThrowIfCancellationRequested();
            if (!e.IsContinueEventChain) break;

            // Filter by source type
            if (cmd.SourceType.HasValue && cmd.SourceType.Value != e.Message.SourceType)
                continue;

            // Check permission
            if (cmd.PermissionLevel > MemberRole.Member && e.Member is not null)
                if (e.Member.Role < cmd.PermissionLevel)
                    continue;

            // Try matching
            if (!_matchers.TryGetValue(cmd.MatchType, out ICommandMatcher? matcher))
                continue;

            if (cmd.Expressions.Select(expr => BuildFullExpression(cmd, expr))
                   .Any(fullExpr => matcher.IsMatch(text, fullExpr)))
            {
                _logger.LogInformation(
                    "Matched command [{CommandName}] via {MatchType} ",
                    cmd.Method.Name,
                    cmd.MatchType);

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

                using CommandExecutionScope executionScope = cmd.PreventReentry
                    ? new CommandExecutionScope(_activeExecutions, executionKey)
                    : default;
                await ExecuteMatchedCommandAsync(cmd, e, ct);

                if (cmd.BlockAfterMatch) e.IsContinueEventChain = false;
            }
        }
    }

    private async ValueTask ExecuteMatchedCommandAsync(
        CommandInfo cmd, MessageReceivedEvent e, CancellationToken ct)
    {
        CommandFilterContext filterContext = CreateFilterContext(cmd);
        bool shortCircuited = await ExecuteBeforeFiltersAsync(cmd, e, filterContext, ct);
        Exception? commandException = shortCircuited
            ? null
            : await ExecuteCommandHandlerAsync(cmd, e, ct);
        await ExecuteAfterFiltersAsync(cmd, e, filterContext, shortCircuited, commandException, ct);
    }

    private async ValueTask<bool> ExecuteBeforeFiltersAsync(
        CommandInfo cmd, MessageReceivedEvent e, CommandFilterContext filterContext, CancellationToken ct)
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
        CommandInfo cmd, MessageReceivedEvent e, CancellationToken ct)
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
        CommandInfo cmd, MessageReceivedEvent e, CommandFilterContext filterContext,
        bool shortCircuited, Exception? commandException, CancellationToken ct)
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
            + "registering an instance via RegisterCommandInstance<T>()",
            type.FullName);
        return RuntimeHelpers.GetUninitializedObject(type);
    }

#endregion
}
