namespace Sora.Entities.Events;

/// <summary>Raised when a group essence message is set or removed.</summary>
public sealed record GroupEssenceChangedEvent : BotEvent
{
    /// <summary>The group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>True if set as essence, false if removed.</summary>
    public bool IsSet { get; init; }

    /// <summary>The message that was set/removed as essence.</summary>
    public MessageId MessageId { get; init; }

    /// <summary>The operator who changed the essence status.</summary>
    public UserId OperatorId { get; init; }

    /// <summary>The original sender of the message.</summary>
    public UserId SenderId { get; init; }
}