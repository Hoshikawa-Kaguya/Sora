using System;
using Newtonsoft.Json;
using Sora.Util;

namespace Sora.Entities.Info;

/// <summary>
/// 群文件夹信息
/// </summary>
public readonly struct GroupFolderInfo
{
    /// <summary>
    /// 文件夹ID
    /// </summary>
    [JsonProperty(PropertyName = "folder_id")]
    public string Id { get; internal init; }

    /// <summary>
    /// 文件夹名
    /// </summary>
    [JsonProperty(PropertyName = "folder_name")]
    public string Name { get; internal init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonIgnore]
    public DateTime CreateTime => CreateTimeStamp.ToDateTime();

    [JsonProperty(PropertyName = "create_time")]
    private long CreateTimeStamp { get; init; }

    /// <summary>
    /// 创建者UID
    /// </summary>
    [JsonProperty(PropertyName = "creator")]
    public long CreatorUserId { get; internal init; }

    /// <summary>
    /// 创建者名
    /// </summary>
    [JsonProperty(PropertyName = "creator_name")]
    public string CreatorUserName { get; internal init; }

    /// <summary>
    /// 子文件数量
    /// </summary>
    [JsonProperty(PropertyName = "total_file_count")]
    public int FileCount { get; internal init; }
}