using System.Reflection;
using Sora.Command.InternalEntities;

namespace Sora.Command;

/// <summary>
///     Internal representation of a registered command.
/// </summary>
internal sealed class CommandInfo
{
    /// <summary>Block event chain after match.</summary>
    public bool BlockAfterMatch { get; init; } = true;

    /// <summary>Unique identifier for dynamic commands (used for unregistration).</summary>
    public Guid CommandId { get; init; }

    /// <summary>Description.</summary>
    public string Description { get; init; } = "";

    /// <summary>Dynamic handler delegate (for runtime-registered commands).</summary>
    public Func<MessageReceivedEvent, ValueTask>? DynamicHandler { get; init; }

    /// <summary>Match expressions.</summary>
    public required string[] Expressions { get; init; }

    /// <summary>Command prefix.</summary>
    public string CommandPrefix { get; init; } = "";

    /// <summary>Singleton instance for instance method invocation (null for static methods).</summary>
    public object? Instance { get; init; }

    /// <summary>Match type.</summary>
    public required MatchType MatchType { get; init; }

    /// <summary>The method to invoke.</summary>
    public required MethodInfo Method { get; init; }

    /// <summary>Required permission level.</summary>
    public MemberRole PermissionLevel { get; init; }

    /// <summary>Requires a super user in addition to the group member permission.</summary>
    public bool SuperUserOnly { get; init; }

    /// <summary>Priority (higher = first).</summary>
    public int Priority { get; init; }

    /// <summary>
    ///     Prevents the same user from triggering this command while a previous invocation is still running. Default is
    ///     <c>true</c>.
    /// </summary>
    public bool PreventReentry { get; init; } = true;

    /// <summary>Optional plain-text reply sent when the command is rejected due to re-entry.</summary>
    public string ReentryMessage { get; init; } = "";

    /// <summary>Message sources the command accepts.</summary>
    public MessageSourceScope SourceScope { get; init; } = MessageSourceScope.Any;

    /// <summary>The assembly registration that owns this command.</summary>
    public required AssemblyRegistration Registration { get; init; }

    /// <summary>
    ///     The registration owner type used for type-level enabling. Scanned commands use the scanned type;
    ///     dynamic commands use the explicitly supplied owner type.
    /// </summary>
    public required Type OwnerType { get; init; }

    /// <summary>Manager-wide registration sequence used to order commands with equal priority.</summary>
    public required long Sequence { get; init; }

    /// <summary>
    ///     Pinned before-filter attribute instances for this command, sorted ascending by Order
    ///     with class-level attributes preceding method-level on Order ties.
    ///     Class-level instances are shared across all commands in the same group.
    /// </summary>
    public IReadOnlyList<CommandBeforeFilterAttribute> BeforeFilters { get; init; } = [];

    /// <summary>
    ///     Pinned after-filter attribute instances for this command, sorted ascending by Order
    ///     with class-level attributes preceding method-level on Order ties.
    ///     Class-level instances are shared across all commands in the same group.
    /// </summary>
    public IReadOnlyList<CommandAfterFilterAttribute> AfterFilters { get; init; } = [];

    /// <summary>
    ///     All custom attributes resolved at registration time (method + declaring type for static commands;
    ///     framework-supplied subset for dynamic commands). Reused for <see cref="CommandFilterContext.Attributes" />
    ///     so filters and the context observe the same instances.
    /// </summary>
    public IReadOnlyList<Attribute> Attributes { get; init; } = [];
}