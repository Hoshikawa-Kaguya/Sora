namespace Sora.Entities.Events;

/// <summary>Raised when a message is recalled/deleted.</summary>
public sealed record MessageDeletedEvent : BotEvent
{
    /// <summary>Recall display suffix text (e.g. "recalled a message").</summary>
    public string DisplaySuffix { get; init; } = "";

    /// <summary>Group ID (default for private messages).</summary>
    public GroupId GroupId { get; init; }

    /// <summary>ID of the deleted message.</summary>
    public MessageId MessageId { get; init; }

    /// <summary>User who performed the deletion.</summary>
    public UserId OperatorId { get; init; }

    /// <summary>Original sender of the message.</summary>
    public UserId SenderId { get; init; }

    /// <summary>Whether this was a private or group message.</summary>
    public MessageSourceType SourceType { get; init; }
}