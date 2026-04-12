namespace Sora.Command.Attributes;

/// <summary>
///     Marks a method as a command handler. Supports both static and instance methods.
///     Instance methods use singleton instances per declaring type.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>Whether to block the event chain after this command matches. Default is <c>true</c></summary>
    public bool BlockAfterMatch { get; set; } = true;

    /// <summary>Description for help text.</summary>
    public string Description { get; set; } = "";

    /// <summary>Match expressions (regex patterns, keywords, or full-text strings).</summary>
    public string[] Expressions { get; set; } = [];

    /// <summary>Matching strategy.</summary>
    public MatchType MatchType { get; set; } = MatchType.Full;

    /// <summary>Minimum member role required to execute this command.</summary>
    public MemberRole PermissionLevel { get; set; } = MemberRole.Member;

    /// <summary>Higher priority commands are matched first.</summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Prevents the same user from triggering this command again while a previous invocation is still executing.
    ///     When enabled, a second trigger from the same user/context is silently skipped (with a warning log).
    ///     This also covers continuous-dialog scenarios where the handler is awaiting <c>WaitForNextMessageAsync</c>.
    ///     Default is <c>true</c>.
    /// </summary>
    public bool PreventReentry { get; set; } = true;

    /// <summary>
    ///     Optional plain-text message sent to the user when their command is rejected due to re-entry.
    ///     Only effective when <see cref="PreventReentry" /> is <c>true</c>.
    ///     Leave empty (default) for silent rejection (log only).
    /// </summary>
    public string ReentryMessage { get; set; } = "";

    /// <summary>Required message source type (null = any).</summary>
    public MessageSourceType? SourceType { get; set; }
}