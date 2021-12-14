using System;
using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 慢速模式信息
/// </summary>
public readonly struct SlowModeInfo
{
    /// <summary>
    /// 慢速模式Key
    /// </summary>
    [JsonProperty(PropertyName = "slow_mode_key")]
    public int SlowModeKey { get; internal init; }

    /// <summary>
    /// 慢速模式说明
    /// </summary>
    [JsonProperty(PropertyName = "slow_mode_text")]
    public string SlowModeText { get; internal init; }

    /// <summary>
    /// 周期内发言频率限制
    /// </summary>
    [JsonProperty(PropertyName = "speak_frequency")]
    public int SpeakFrequency { get; internal init; }

    /// <summary>
    /// 单位周期时间, 单位秒
    /// </summary>
    [JsonProperty(PropertyName = "slow_mode_circle")]
    internal int SlowModeCircleSecond
    {
        get => (int) SlowModeCircle.TotalSeconds;
        init => SlowModeCircle = TimeSpan.FromSeconds(value);
    }

    /// <summary>
    /// 单位周期时间, 单位秒
    /// </summary>
    [JsonIgnore]
    public TimeSpan SlowModeCircle { get; internal init; }
}