namespace Sora.Entities.Info;

/// <summary>Group announcement information.</summary>
public sealed record GroupAnnouncementInfo
{
    /// <summary>Group ID.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Publisher's user ID.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>Announcement unique identifier.</summary>
    public string AnnouncementId { get; internal init; } = "";

    /// <summary>Announcement content.</summary>
    public string Content { get; internal init; } = "";

    /// <summary>Announcement image URL (optional).</summary>
    public string? ImageUrl { get; internal init; }

    /// <summary>Publish timestamp.</summary>
    public DateTime Time { get; internal init; }
}