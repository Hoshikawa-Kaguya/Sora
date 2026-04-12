namespace Sora.Entities.Events;

/// <summary>Raised when a file is uploaded (private or group).</summary>
public sealed record FileUploadEvent : BotEvent
{
    /// <summary>File hash (may be empty if unavailable).</summary>
    public string FileHash { get; init; } = "";

    /// <summary>File identifier.</summary>
    public string FileId { get; init; } = "";

    /// <summary>File name.</summary>
    public string FileName { get; init; } = "";

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; init; }

    /// <summary>Group ID (default for private).</summary>
    public GroupId GroupId { get; init; }

    /// <summary>Whether this file was sent by the bot itself (private uploads only).</summary>
    public bool IsSelfSent { get; init; }

    /// <summary>Whether this was a private or group upload.</summary>
    public MessageSourceType SourceType { get; init; }

    /// <summary>The user who uploaded the file.</summary>
    public UserId UserId { get; init; }
}