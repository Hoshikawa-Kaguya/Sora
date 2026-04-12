namespace Sora.Entities.Events;

/// <summary>Raised when a friend add request is received.</summary>
public sealed record FriendRequestEvent : BotEvent
{
    /// <summary>Verification message.</summary>
    public string Comment { get; init; } = "";

    /// <summary>The user requesting to be added as friend.</summary>
    public UserId FromUserId { get; init; }

    /// <summary>Whether this request is filtered (suspicious/low-trust).</summary>
    public bool IsFiltered { get; init; }

    /// <summary>The source/channel of the friend request.</summary>
    public string Via { get; init; } = "";
}