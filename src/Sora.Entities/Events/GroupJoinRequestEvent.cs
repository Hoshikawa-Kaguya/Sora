namespace Sora.Entities.Events;

/// <summary>Raised when a user requests to join a group (directly or via invitation).</summary>
public sealed record GroupJoinRequestEvent : BotEvent
{
    /// <summary>Verification message.</summary>
    public string Comment { get; init; } = "";

    /// <summary>The user requesting to join.</summary>
    public UserId FromUserId { get; init; }

    /// <summary>The group being requested to join.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>The user who invited the requester (default if no inviter). go-cqhttp extension.</summary>
    public UserId InvitorId { get; init; }

    /// <summary>Whether this notification is filtered (suspicious/low-trust).</summary>
    public bool IsFiltered { get; init; }

    /// <summary>The type of group join notification.</summary>
    public GroupJoinNotificationType JoinNotificationType { get; init; }

    /// <summary>The notification sequence number for handling this request.</summary>
    public long NotificationSeq { get; init; }
}