using System;
using Newtonsoft.Json;
using Sora.Util;


namespace Sora.Entities.Info;

/// <summary>
/// 群信息
/// </summary>
public readonly struct GroupInfo
{
    #region 属性

    /// <summary>
    /// 群名称
    /// </summary>
    [JsonProperty(PropertyName = "group_name")]
    public string GroupName { get; internal init; }

    /// <summary>
    /// 成员数
    /// </summary>
    [JsonProperty(PropertyName = "member_count")]
    public int MemberCount { get; internal init; }

    /// <summary>
    /// 最大成员数（群容量）
    /// </summary>
    [JsonProperty(PropertyName = "max_member_count")]
    public int MaxMemberCount { get; internal init; }

    /// <summary>
    /// 群组ID
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    public long GroupId { get; internal init; }

    /// <summary>
    /// 群备注
    /// </summary>
    [JsonProperty(PropertyName = "group_memo", NullValueHandling = NullValueHandling.Ignore)]
    public string GroupMemo { get; internal init; }

    /// <summary>
    /// 群创建时间
    /// </summary>
    [JsonIgnore]
    public DateTime? GroupCreateTime { get; internal init; }

    /// <summary>
    /// 群创建时间
    /// </summary>
    [JsonProperty(PropertyName = "group_create_time", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private long? GroupCreateTimeStamp
    {
        get => (GroupCreateTime ?? DateTime.MinValue).ToTimeStamp();
        init => (value          ?? default).ToDateTime();
    }

    /// <summary>
    /// 群等级
    /// </summary>
    [JsonProperty(PropertyName = "group_level", NullValueHandling = NullValueHandling.Ignore)]
    public int? GroupLevel { get; internal init; }

    #endregion
}