namespace Sora.Entities.Events;

/// <summary>Raised when a group admin is added or removed.</summary>
public sealed record GroupAdminChangedEvent : BotEvent
{
    /// <summary>The group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>True if promoted to admin, false if demoted.</summary>
    public bool IsSet { get; init; }

    /// <summary>The operator (group owner).</summary>
    public UserId OperatorId { get; init; }

    /// <summary>The user whose admin status changed.</summary>
    public UserId UserId { get; init; }
}