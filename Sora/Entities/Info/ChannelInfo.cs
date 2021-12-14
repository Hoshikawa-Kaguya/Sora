using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Enumeration;
using Sora.Util;

namespace Sora.Entities.Info;

/// <summary>
/// 
/// </summary>
public readonly struct ChannelInfo
{
    /// <summary>
    /// 所属频道ID
    /// </summary>
    [JsonProperty(PropertyName = "owner_guild_id")]
    public ulong OwnerGuildId { get; internal init; }

    /// <summary>
    /// 子频道ID
    /// </summary>
    [JsonProperty(PropertyName = "channel_id")]
    public ulong ChannelId { get; internal init; }

    /// <summary>
    /// 子频道类型
    /// </summary>
    //此字段的json类型为int32，不需要字符串转换
    [JsonProperty(PropertyName = "channel_type")]
    public ChannelType ChannelType { get; internal init; }

    /// <summary>
    /// 子频道名称
    /// </summary>
    [JsonProperty(PropertyName = "channel_name")]
    public string channel_name { get; internal init; }

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
    public DateTime CreateTime { get; init; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [JsonProperty(PropertyName = "creator_id")]
    public long CreatorUserId { get; internal init; }

    /// <summary>
    /// 创建者频道ID
    /// </summary>
    [JsonProperty(PropertyName = "creator_tiny_id")]
    public string CreatorGuildId { get; internal init; }

    //TODO 作用模糊的字段
    /// <summary>
    /// <para>发言权限类型</para>
    /// </summary>
    [JsonProperty(PropertyName = "talk_permission")]
    public int TalkPermission { get; internal init; }

    /// <summary>
    /// <para>可视性类型</para>
    /// </summary>
    [JsonProperty(PropertyName = "visible_type")]
    public int VisibleType { get; internal init; }

    /// <summary>
    /// 当前启用的慢速模式Key
    /// </summary>
    [JsonProperty(PropertyName = "current_slow_mode")]
    public int CurrentSlowMode { get; internal init; }

    /// <summary>
    /// 频道内可用慢速模式类型列表
    /// </summary>
    [JsonProperty(PropertyName = "slow_modes")]
    public List<SlowModeInfo> SlowModes { get; internal init; }
}