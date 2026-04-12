namespace Sora.Entities.Info;

/// <summary>Group notification information (join requests, admin changes, kicks, etc.).</summary>
public sealed record GroupNotificationInfo
{
    /// <summary>Group ID.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Notification sequence number.</summary>
    public long NotificationSeq { get; internal init; }

    /// <summary>Initiator/target user ID (context-dependent).</summary>
    public UserId InitiatorId { get; internal init; }

    /// <summary>Operator user ID (optional).</summary>
    public UserId? OperatorId { get; internal init; }

    /// <summary>Target user ID (for kicks, invited_join_request).</summary>
    public UserId TargetUserId { get; internal init; }

    /// <summary>Notification type: join_request, admin_change, kick, quit, invited_join_request.</summary>
    public string Type { get; internal init; } = "";

    /// <summary>Request state: pending, accepted, rejected, ignored.</summary>
    public string? State { get; internal init; }

    /// <summary>Comment/reason.</summary>
    public string? Comment { get; internal init; }

    /// <summary>Whether the notification is filtered (from risky account).</summary>
    public bool IsFiltered { get; internal init; }

    /// <summary>Whether admin was set (for admin_change).</summary>
    public bool IsSet { get; internal init; }
}