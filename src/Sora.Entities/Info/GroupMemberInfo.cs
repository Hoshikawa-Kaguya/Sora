namespace Sora.Entities.Info;

/// <summary>Group member information.</summary>
public sealed record GroupMemberInfo
{
    /// <summary>Group the member belongs to.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Member's user ID.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>Member's nickname.</summary>
    public string Nickname { get; internal init; } = "";

    /// <summary>Member's group card/remark.</summary>
    public string Card { get; internal init; } = "";

    /// <summary>Member's special title.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Member's role in the group.</summary>
    public MemberRole Role { get; internal init; }

    /// <summary>Member's gender.</summary>
    public Sex Sex { get; internal init; }

    /// <summary>Member's age.</summary>
    public int Age { get; internal init; }

    /// <summary>Member's group level.</summary>
    public int Level { get; internal init; }

    /// <summary>When the member joined the group.</summary>
    public DateTime? JoinTime { get; internal init; }

    /// <summary>When the member last sent a message.</summary>
    public DateTime? LastSentTime { get; internal init; }

    /// <summary>When the member's mute expires (null if not muted).</summary>
    public DateTime? MuteExpireTime { get; internal init; }
}