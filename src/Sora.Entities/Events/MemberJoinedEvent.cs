namespace Sora.Entities.Events;

/// <summary>Raised when a new member joins a group.</summary>
public sealed record MemberJoinedEvent : BotEvent
{
    /// <summary>The group that was joined.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>User who invited the new member (if applicable).</summary>
    public UserId? InvitorId { get; init; }

    /// <summary>Admin who approved the join (if applicable).</summary>
    public UserId? OperatorId { get; init; }

    /// <summary>Protocol-level sub type (e.g., "approve", "invite").</summary>
    public string SubType { get; init; } = "";

    /// <summary>The user who joined.</summary>
    public UserId UserId { get; init; }
}