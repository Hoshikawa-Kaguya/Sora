namespace Sora.Entities.Events;

/// <summary>Raised when a new friend is added.</summary>
public sealed record FriendAddedEvent : BotEvent
{
    /// <summary>The new friend's user ID.</summary>
    public UserId UserId { get; init; }
}