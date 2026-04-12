namespace Sora.Entities.Info;

/// <summary>Result of a group file listing operation.</summary>
public sealed record GroupFilesResult
{
    /// <summary>Files in the directory.</summary>
    public IReadOnlyList<GroupFileInfo> Files { get; internal init; } = [];

    /// <summary>Folders in the directory.</summary>
    public IReadOnlyList<GroupFolderInfo> Folders { get; internal init; } = [];
}

/// <summary>Group file information.</summary>
public sealed record GroupFileInfo
{
    /// <summary>Group identifier.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>File identifier.</summary>
    public string FileId { get; internal init; } = "";

    /// <summary>Parent folder ID.</summary>
    public string ParentFolderId { get; internal init; } = "";

    /// <summary>Uploader's user ID.</summary>
    public UserId UploaderId { get; internal init; }

    /// <summary>File name.</summary>
    public string FileName { get; internal init; } = "";

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; internal init; }

    /// <summary>Download count.</summary>
    public int DownloadedTimes { get; internal init; }

    /// <summary>Upload timestamp.</summary>
    public DateTime? UploadedTime { get; internal init; }

    /// <summary>Expiration timestamp.</summary>
    public DateTime? ExpireTime { get; internal init; }
}

/// <summary>Group folder information.</summary>
public sealed record GroupFolderInfo
{
    /// <summary>Group identifier.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Folder identifier.</summary>
    public string FolderId { get; internal init; } = "";

    /// <summary>Parent folder ID.</summary>
    public string ParentFolderId { get; internal init; } = "";

    /// <summary>Creator's user ID.</summary>
    public UserId CreatorId { get; internal init; }

    /// <summary>Folder name.</summary>
    public string FolderName { get; internal init; } = "";

    /// <summary>Number of files in this folder.</summary>
    public int FileCount { get; internal init; }

    /// <summary>Creation timestamp.</summary>
    public DateTime? CreatedTime { get; internal init; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime? LastModifiedTime { get; internal init; }
}