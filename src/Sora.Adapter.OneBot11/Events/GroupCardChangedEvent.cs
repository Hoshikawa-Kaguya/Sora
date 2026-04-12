namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when a member's group card (nickname) changes. OB11-specific.</summary>
public sealed record GroupCardChangedEvent : BotEvent
{
    /// <summary>User whose card changed.</summary>
    public UserId UserId { get; init; }

    /// <summary>Group where the card changed.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>Previous group card.</summary>
    public string CardOld { get; init; } = "";

    /// <summary>New group card.</summary>
    public string CardNew { get; init; } = "";
}