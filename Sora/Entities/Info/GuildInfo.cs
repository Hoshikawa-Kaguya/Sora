using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 频道信息
/// </summary>
public readonly struct GuildInfo
{
    /// <summary>
    /// 频道ID
    /// </summary>
    [JsonProperty(PropertyName = "guild_id")]
    public ulong Id { get; internal init; }

    /// <summary>
    /// 频道名
    /// </summary>
    [JsonProperty(PropertyName = "guild_name")]
    public string Name { get; internal init; }

    /// <summary>
    /// 频道显示ID
    /// </summary>
    [JsonProperty(PropertyName = "guild_display_id")]
    public long DisplayId { get; internal init; }
}