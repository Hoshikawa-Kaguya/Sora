namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when a group is dismissed (disbanded). OB11-specific.</summary>
public sealed record GroupDismissedEvent : BotEvent
{
    /// <summary>Group that was dismissed.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>User who dismissed the group (typically the owner).</summary>
    public UserId OperatorId { get; init; }
}