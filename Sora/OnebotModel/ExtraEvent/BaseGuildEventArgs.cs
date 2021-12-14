using Newtonsoft.Json;

namespace Sora.OnebotModel.ExtraEvent;

/// <summary>
/// 频道基础字段
/// </summary>
internal abstract class BaseGuildEventArgs : BaseEventArgs
{
    /// <summary>
    /// 频道ID
    /// </summary>
    [JsonProperty(PropertyName = "guild_id")]
    internal ulong GuildId { get; set; }

    /// <summary>
    /// 子频道ID
    /// </summary>
    [JsonProperty(PropertyName = "channel_id")]
    internal ulong ChannelId { get; set; }

    /// <summary>
    /// 发送者ID
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    internal ulong UserGuildId { get; set; }

    /// <summary>
    /// <para>操作者/发送者用户ID</para>
    /// <para>在频道系统中代表用户ID</para>
    /// <para>与QQ号并不通用</para>
    /// </summary>
    [JsonProperty(PropertyName = "self_tiny_id")]
    internal ulong SelfGuildId { get; set; }
}