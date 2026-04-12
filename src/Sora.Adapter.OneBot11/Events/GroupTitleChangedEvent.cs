namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when a group member's special title is changed. OB11-specific.</summary>
public sealed record GroupTitleChangedEvent : BotEvent
{
    /// <summary>User who received the title.</summary>
    public UserId UserId { get; init; }

    /// <summary>Group where the title changed.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>New special title.</summary>
    public string Title { get; init; } = "";
}