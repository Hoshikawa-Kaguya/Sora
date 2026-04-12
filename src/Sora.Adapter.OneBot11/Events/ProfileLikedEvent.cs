namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when the bot's profile is liked. OB11-specific.</summary>
public sealed record ProfileLikedEvent : BotEvent
{
    /// <summary>User who liked the profile.</summary>
    public UserId SenderId { get; init; }

    /// <summary>Display name of the user who liked.</summary>
    public string OperatorNickname { get; init; } = "";

    /// <summary>Number of likes given.</summary>
    public int Times { get; init; }
}