using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models.Api;

/// <summary>Input parameters for the upload_private_file API.</summary>
internal sealed class UploadPrivateFileInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("file_name")]
    public string FileName { get; set; } = "";

    [JsonProperty("file_uri")]
    public string FileUri { get; set; } = "";
}

/// <summary>Input parameters for the upload_group_file API.</summary>
internal sealed class UploadGroupFileInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("parent_folder_id")]
    public string ParentFolderId { get; set; } = "/";

    [JsonProperty("file_name")]
    public string FileName { get; set; } = "";

    [JsonProperty("file_uri")]
    public string FileUri { get; set; } = "";
}

/// <summary>Output data from file upload APIs.</summary>
internal sealed class UploadFileOutput
{
    [JsonProperty("file_id")]
    public string? FileId { get; set; }
}

/// <summary>Input parameters for the get_private_file_download_url API.</summary>
internal sealed class GetPrivateFileDownloadUrlInput
{
    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("file_hash")]
    public string FileHash { get; set; } = "";
}

/// <summary>Input parameters for the get_group_file_download_url API.</summary>
internal sealed class GetGroupFileDownloadUrlInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";
}

/// <summary>Output data from file download URL APIs.</summary>
internal sealed class DownloadUrlOutput
{
    [JsonProperty("download_url")]
    public string? DownloadUrl { get; set; }
}

/// <summary>Input parameters for the get_group_files API.</summary>
internal sealed class GetGroupFilesInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("parent_folder_id")]
    public string ParentFolderId { get; set; } = "/";
}

/// <summary>Output data from the get_group_files API.</summary>
internal sealed class GetGroupFilesOutput
{
    [JsonProperty("files")]
    public List<MilkyGroupFileEntity> Files { get; set; } = [];

    [JsonProperty("folders")]
    public List<MilkyGroupFolderEntity> Folders { get; set; } = [];
}

/// <summary>Input parameters for the move_group_file API.</summary>
internal sealed class MoveGroupFileInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("parent_folder_id")]
    public string ParentFolderId { get; set; } = "/";

    [JsonProperty("target_folder_id")]
    public string TargetFolderId { get; set; } = "/";
}

/// <summary>Input parameters for the rename_group_file API.</summary>
internal sealed class RenameGroupFileInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";

    [JsonProperty("parent_folder_id")]
    public string ParentFolderId { get; set; } = "/";

    [JsonProperty("new_file_name")]
    public string NewFileName { get; set; } = "";
}

/// <summary>Input parameters for the delete_group_file API.</summary>
internal sealed class DeleteGroupFileInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("file_id")]
    public string FileId { get; set; } = "";
}

/// <summary>Input parameters for the create_group_folder API.</summary>
internal sealed class CreateGroupFolderInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_name")]
    public string FolderName { get; set; } = "";
}

/// <summary>Output data from the create_group_folder API.</summary>
internal sealed class CreateGroupFolderOutput
{
    [JsonProperty("folder_id")]
    public string? FolderId { get; set; }
}

/// <summary>Input parameters for the rename_group_folder API.</summary>
internal sealed class RenameGroupFolderInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_id")]
    public string FolderId { get; set; } = "";

    [JsonProperty("new_folder_name")]
    public string NewFolderName { get; set; } = "";
}

/// <summary>Input parameters for the delete_group_folder API.</summary>
internal sealed class DeleteGroupFolderInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("folder_id")]
    public string FolderId { get; set; } = "";
}