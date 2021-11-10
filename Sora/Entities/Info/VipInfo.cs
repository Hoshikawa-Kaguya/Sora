using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// VIP信息
/// </summary>
public readonly struct VipInfo
{
    /// <summary>
    /// 用户id
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    public long UserId { get; internal init; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonProperty(PropertyName = "nickname")]
    public string Nick { get; internal init; }

    /// <summary>
    /// 等级
    /// </summary>
    [JsonProperty(PropertyName = "level")]
    public long Level { get; internal init; }

    /// <summary>
    /// 等级加速度
    /// </summary>
    [JsonProperty(PropertyName = "level_speed")]
    public double LevelSpeed { get; internal init; }

    /// <summary>
    /// 会员等级
    /// </summary>
    [JsonProperty(PropertyName = "vip_level")]
    public string VipLevel { get; internal init; }

    /// <summary>
    /// 会员成长速度
    /// </summary>
    [JsonProperty(PropertyName = "vip_growth_speed")]
    public long VipGrowthSpeed { get; internal init; }

    /// <summary>
    /// 会员成长总值
    /// </summary>
    [JsonProperty(PropertyName = "vip_growth_total")]
    public long VipGrowthTotal { get; internal init; }
}