namespace Sora.Entities.Info;

/// <summary>Result of a group notifications query.</summary>
public sealed record GroupNotificationsResult
{
    /// <summary>Next page start notification sequence (null if no more).</summary>
    public long? NextNotificationSeq { get; internal init; }

    /// <summary>Group notifications (notification_seq descending).</summary>
    public IReadOnlyList<GroupNotificationInfo> Notifications { get; internal init; } = [];
}