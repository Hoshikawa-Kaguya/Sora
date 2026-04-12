using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sora.Command;

/// <summary>
///     Discovers, registers, and executes commands based on message content.
/// </summary>
public sealed class CommandManager
{
#region Fields

    private readonly List<CommandInfo>                                _commands   = [];
    private readonly ConcurrentDictionary<Type, object>               _instances  = new();
    private readonly Lock                                             _lock       = new();
    private readonly Lazy<ILogger>                                    _loggerLazy = new(SoraLogger.CreateLogger<CommandManager>);
    private          ILogger                                          _logger => _loggerLazy.Value;
    private readonly ConcurrentDictionary<MatchType, ICommandMatcher> _matchers     = new();
    private readonly HashSet<Type>                                    _scannedTypes = [];
    private          bool                                             _needsSort;

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
    public T? GetCommandInstance<T>() where T : class => _instances.TryGetValue(typeof(T), out object? instance) ? (T)instance : null;

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
            throw new InvalidOperationException($"RegisterCommandInstance<{typeof(T).Name}>() must be called before ScanAssembly().");
        _instances[typeof(T)] = instance;
    }

#endregion

#region Command Registration

    /// <summary>
    ///     Dynamically registers a command at runtime.
    /// </summary>
    /// <param name="handler">The handler delegate to invoke.</param>
    /// <param name="expressions">Match expressions.</param>
    /// <param name="matchType">Matching strategy.</param>
    /// <param name="sourceType">Required message source type (null = any).</param>
    /// <param name="permissionLevel">Minimum member role required.</param>
    /// <param name="priority">Higher priority commands are matched first.</param>
    /// <param name="blockAfterMatch">Whether to block the event chain after matching.</param>
    /// <param name="description">Description for help text.</param>
    /// <param name="preventReentry">
    ///     When true, prevents the same user from triggering this command while a previous invocation is still executing.
    /// </param>
    /// <param name="reentryMessage">Optional plain-text reply sent when the command is rejected due to re-entry.</param>
    public void RegisterDynamicCommand(
        Func<MessageReceivedEvent, ValueTask> handler,
        string[]                              expressions,
        MatchType                             matchType       = MatchType.Full,
        MessageSourceType?                    sourceType      = null,
        MemberRole                            permissionLevel = MemberRole.Member,
        int                                   priority        = 0,
        bool                                  blockAfterMatch = true,
        string                                description     = "",
        bool                                  preventReentry  = true,
        string                                reentryMessage  = "")
    {
        CommandInfo info = new()
            {
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
                ReentryMessage  = reentryMessage
            };

        lock (_lock)
        {
            _commands.Add(info);
            _needsSort = true;
        }

        _logger.LogDebug(
            "Registered dynamic command [{CommandName}] via {MatchType} (source: {SourceType}, priority: {Priority}, block: {BlockAfterMatch})",
            handler.Method.Name,
            matchType,
            sourceType,
            priority,
            blockAfterMatch);
    }

    /// <summary>Registers a custom command matcher.</summary>
    /// <param name="matcher">The matcher to register.</param>
    public void RegisterMatcher(ICommandMatcher matcher)
    {
        _matchers[matcher.MatchType] = matcher;
        _logger.LogDebug("Registered command matcher [{MatcherType}] for {MatchType}", matcher.GetType().Name, matcher.MatchType);
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
    public int ScanType(Type type)
    {
        int                    commandCount = 0;
        CommandGroupAttribute? groupAttr    = type.GetCustomAttribute<CommandGroupAttribute>();
        string                 prefix       = groupAttr?.Prefix ?? "";
        _scannedTypes.Add(type);

        _logger.LogDebug("Scanning command type [{TypeName}] with prefix '{Prefix}'", type.FullName, prefix);

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
                instance = _instances.GetOrAdd(
                    type,
                    t =>
                    {
                        // Search public and non-public parameterless constructors
                        ConstructorInfo? ctor = t.GetConstructor(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            Type.EmptyTypes);
                        if (ctor is not null) return ctor.Invoke(null);

                        // No parameterless constructor — create uninitialized instance
                        _logger.LogWarning(
                            "Type {TypeName} has no parameterless constructor — using GetUninitializedObject. "
                            + "Fields will not be initialized. Consider adding a parameterless constructor or "
                            + "registering an instance via RegisterCommandInstance<T>()",
                            t.FullName);
                        return RuntimeHelpers.GetUninitializedObject(t);
                    });

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
                    GroupPrefix     = prefix,
                    Description     = cmdAttr.Description,
                    PreventReentry  = cmdAttr.PreventReentry,
                    ReentryMessage  = cmdAttr.ReentryMessage
                };

            lock (_lock)
            {
                _commands.Add(info);
                _needsSort = true;
            }

            _logger.LogDebug(
                "Registered command [{CommandName}] via {MatchType} (source: {SourceType}, priority: {Priority}, block: {BlockAfterMatch})",
                method.Name,
                cmdAttr.MatchType,
                cmdAttr.SourceType,
                cmdAttr.Priority,
                cmdAttr.BlockAfterMatch);

            commandCount++;
        }

        _logger.LogInformation(
            "Scanned {Count} commands from assembly {TypeName}",
            commandCount,
            type.Name);
        return commandCount;
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

            snapshot = [.._commands];
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

            if (cmd.Expressions.Select(expr => cmd.GroupPrefix + expr)
                   .Any(fullExpr => matcher.IsMatch(text, fullExpr)))
            {
                _logger.LogInformation("Matched command [{CommandName}] via {MatchType} ", cmd.Method.Name, cmd.MatchType);

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

                try
                {
                    if (cmd.DynamicHandler is not null)
                    {
                        await cmd.DynamicHandler(e);
                    }
                    else
                    {
                        object? result = cmd.Method.Invoke(cmd.Instance, [e]);
                        await (result switch
                                   {
                                       ValueTask vt => vt.AsTask(),
                                       Task t => t,
                                       _ => throw new InvalidOperationException("Command method must return Task or ValueTask")
                                   });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command '{CommandName}' threw an unhandled exception", cmd.Method.Name);
                }
                finally
                {
                    if (cmd.PreventReentry)
                        _activeExecutions.TryRemove(executionKey, out _);
                }

                if (cmd.BlockAfterMatch) e.IsContinueEventChain = false;
            }
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

#endregion

#region Nested Types

    /// <summary>
    ///     Identifies a unique in-flight command execution for re-entry protection.
    /// </summary>
    private readonly record struct ExecutionKey(
        MethodInfo        Method,
        Guid              ConnectionId,
        UserId            SenderId,
        GroupId           GroupId,
        MessageSourceType SourceType);

#endregion
}