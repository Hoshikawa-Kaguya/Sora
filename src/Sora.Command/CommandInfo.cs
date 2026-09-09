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

    /// <summary>Priority (higher = first).</summary>
    public int Priority { get; init; }

    /// <summary>
    ///     Prevents the same user from triggering this command while a previous invocation is still running. Default is
    ///     <c>true</c>.
    /// </summary>
    public bool PreventReentry { get; init; } = true;

    /// <summary>Optional plain-text reply sent when the command is rejected due to re-entry.</summary>
    public string ReentryMessage { get; init; } = "";

    /// <summary>Source type filter (null = any).</summary>
    public MessageSourceType? SourceType { get; init; }

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