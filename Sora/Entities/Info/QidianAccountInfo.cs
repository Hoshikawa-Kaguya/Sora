using System;
using Newtonsoft.Json;
using Sora.Util;

namespace Sora.Entities.Info;

/// <summary>
/// 企点账号信息
/// </summary>
public readonly struct QidianAccountInfo
{
    /// <summary>
    /// 父账号ID
    /// </summary>
    [JsonProperty(PropertyName = "master_id")]
    public long MasterId { get; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    [JsonProperty(PropertyName = "ext_name")]
    public string Name { get; }

    /// <summary>
    /// 账号创建时间戳
    /// </summary>
    [JsonProperty(PropertyName = "create_time")]
    internal long CreateTimeStamp { get; }

    /// <summary>
    /// 账号创建时间
    /// </summary>
    [JsonIgnore]
    public DateTime CreateTime => CreateTimeStamp.ToDateTime();
}