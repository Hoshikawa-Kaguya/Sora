namespace Sora.Entities.Events;

/// <summary>Raised when a member leaves or is kicked from a group.</summary>
public sealed record MemberLeftEvent : BotEvent
{
    /// <summary>The group that was left.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>Whether the member was kicked (true) or left voluntarily (false).</summary>
    public bool IsKicked { get; init; }

    /// <summary>Admin who kicked the member (if applicable).</summary>
    public UserId? OperatorId { get; init; }

    /// <summary>Protocol-level sub type (e.g., "leave", "kick", "kick_me").</summary>
    public string SubType { get; init; } = "";

    /// <summary>The user who left.</summary>
    public UserId UserId { get; init; }
}