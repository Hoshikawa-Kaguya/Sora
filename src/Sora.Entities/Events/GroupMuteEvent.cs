namespace Sora.Entities.Events;

/// <summary>Raised when a group member is muted/unmuted or group-wide mute is toggled.</summary>
public sealed record GroupMuteEvent : BotEvent
{
    /// <summary>Mute duration in seconds (0 means unmute).</summary>
    public int DurationSeconds { get; init; }

    /// <summary>The group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>True if this is a group-wide mute toggle.</summary>
    public bool IsWholeGroup { get; init; }

    /// <summary>The operator who performed the mute.</summary>
    public UserId OperatorId { get; init; }

    /// <summary>The user who was muted (default for group-wide mute).</summary>
    public UserId UserId { get; init; }
}