using System.Reflection;

namespace Sora.Command;

/// <summary>
///     Registers dynamic commands on behalf of one <see cref="AssemblyRegistration" />.
///     Hosts can hand this narrow view to plugins so that plugins can add commands without receiving
///     enable, priority or unregistration control. This is an API convention, not a security boundary.
/// </summary>
public interface ICommandRegistrar
{
    /// <summary>The assembly that owns every command registered through this registrar.</summary>
    Assembly Assembly { get; }

    /// <summary>
    ///     Registers a command at runtime. The command belongs to <paramref name="ownerType" /> inside this registrar's
    ///     assembly: it follows that assembly's enabled state and priority and that type's enabled state, and is removed
    ///     when the assembly registration is unregistered. The command is registered even while the assembly or type is
    ///     disabled and starts matching once both are enabled.
    /// </summary>
    /// <param name="ownerType">
    ///     The type that owns the command. It must be defined in <see cref="Assembly" />; types from Sora, the runtime
    ///     or other assemblies (including base-class assemblies) are rejected. The assembly containing the handler code is
    ///     not inspected.
    /// </param>
    /// <param name="handler">The async handler to invoke when the command matches.</param>
    /// <param name="expressions">One or more match expressions.</param>
    /// <param name="matchType">How to match the expressions against message text.</param>
    /// <param name="sourceScope">Message sources the command accepts. Defaults to <see cref="MessageSourceScope.Any" />.</param>
    /// <param name="permissionLevel">Minimum member role required.</param>
    /// <param name="priority">Higher priority commands are matched first inside the owning assembly.</param>
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
    /// <param name="superUserOnly">Requires a configured super user in addition to the member role.</param>
    /// <returns>A unique command ID that can be used to unregister this command later.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ownerType" /> or <paramref name="handler" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ownerType" /> is not defined in <see cref="Assembly" />.</exception>
    /// <exception cref="InvalidOperationException">The assembly registration has been unregistered.</exception>
    Guid RegisterDynamicCommand(
        Type                                  ownerType,
        Func<MessageReceivedEvent, ValueTask> handler,
        string[]                              expressions,
        MatchType                             matchType       = MatchType.Full,
        MessageSourceScope                    sourceScope     = MessageSourceScope.Any,
        MemberRole                            permissionLevel = MemberRole.Member,
        int                                   priority        = 0,
        bool                                  blockAfterMatch = true,
        string                                description     = "",
        bool                                  preventReentry  = true,
        string                                reentryMessage  = "",
        string                                prefix          = "",
        CommandBeforeFilterAttribute[]?       beforeFilters   = null,
        CommandAfterFilterAttribute[]?        afterFilters    = null,
        bool                                  superUserOnly   = false);

    /// <summary>
    ///     Unregisters a dynamic command registered through this registrar. Messages that already read the previous
    ///     command table may still invoke it once.
    /// </summary>
    /// <param name="commandId">The command ID returned by <see cref="RegisterDynamicCommand" />.</param>
    /// <returns>
    ///     <c>true</c> if the command was found in this registration and removed; <c>false</c> if it does not belong to
    ///     this registration or the registration has been unregistered.
    /// </returns>
    bool UnregisterDynamicCommand(Guid commandId);
}