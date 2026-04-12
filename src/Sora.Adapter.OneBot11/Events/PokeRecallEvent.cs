namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when a friend poke is recalled. OB11-specific.</summary>
public sealed record FriendPokeRecallEvent : BotEvent
{
    /// <summary>The friend user who recalled the poke.</summary>
    public UserId UserId { get; internal init; }
}

/// <summary>Raised when a group poke is recalled. OB11-specific.</summary>
public sealed record GroupPokeRecallEvent : BotEvent
{
    /// <summary>Group where the poke was recalled.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>User who recalled the poke.</summary>
    public UserId UserId { get; internal init; }
}