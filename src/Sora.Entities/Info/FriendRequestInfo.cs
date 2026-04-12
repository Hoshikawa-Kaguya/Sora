namespace Sora.Entities.Info;

/// <summary>Friend request information.</summary>
public sealed record FriendRequestInfo
{
    /// <summary>Request initiator's user ID.</summary>
    public UserId InitiatorId { get; internal init; }

    /// <summary>Request initiator's UID (for accept/reject).</summary>
    public string InitiatorUid { get; internal init; } = "";

    /// <summary>Target user's ID.</summary>
    public UserId TargetUserId { get; internal init; }

    /// <summary>Target user's UID.</summary>
    public string TargetUserUid { get; internal init; } = "";

    /// <summary>Request state: pending, accepted, rejected, ignored.</summary>
    public string State { get; internal init; } = "";

    /// <summary>Request message/comment.</summary>
    public string Comment { get; internal init; } = "";

    /// <summary>Request source (e.g. search, group).</summary>
    public string Via { get; internal init; } = "";

    /// <summary>Request timestamp.</summary>
    public DateTime Time { get; internal init; }

    /// <summary>Whether the request is from a flagged/risky account.</summary>
    public bool IsFiltered { get; internal init; }
}