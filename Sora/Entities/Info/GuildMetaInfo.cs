using System;
using Newtonsoft.Json;
using Sora.Util;

namespace Sora.Entities.Info;

/// <summary>
/// 频道元数据
/// </summary>
public readonly struct GuildMetaInfo
{
    /// <summary>
    /// 频道ID
    /// </summary>
    [JsonProperty(PropertyName = "guild_id")]
    public ulong GuildId { get; internal init; }

    /// <summary>
    /// 频道名
    /// </summary>
    [JsonProperty(PropertyName = "guild_name")]
    public string GuildName { get; internal init; }

    /// <summary>
    /// 频道简介
    /// </summary>
    [JsonProperty(PropertyName = "guild_profile")]
    public string GuildProfile { get; internal init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonProperty(PropertyName = "create_time")]
    internal long CreateTimeStamp
    {
        get => CreateTime.ToTimeStamp();
        init => CreateTime = value.ToDateTime();
    }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonIgnore]
    public DateTime CreateTime { get; internal init; }

    /// <summary>
    /// 频道人数上限
    /// </summary>
    [JsonProperty(PropertyName = "max_member_count")]
    public long MaxMemberCount { get; internal init; }

    /// <summary>
    /// 频道BOT数上限
    /// </summary>
    [JsonProperty(PropertyName = "max_robot_count")]
    public long MaxRobotCount { get; internal init; }

    /// <summary>
    /// 频道管理员人数上限
    /// </summary>
    [JsonProperty(PropertyName = "max_admin_count")]
    public long MaxAdminCount { get; internal init; }

    /// <summary>
    /// 已加入人数
    /// </summary>
    [JsonProperty(PropertyName = "member_count")]
    public long MemberCount { get; internal init; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [JsonProperty(PropertyName = "owner_id")]
    public ulong OwnerGuildId { get; internal init; }
}