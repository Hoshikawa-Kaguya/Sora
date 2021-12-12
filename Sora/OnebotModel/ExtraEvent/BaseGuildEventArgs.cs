using Newtonsoft.Json;

namespace Sora.OnebotModel.ExtraEvent
{
    /// <summary>
    /// 频道基础字段
    /// </summary>
    internal abstract class BaseGuildEventArgs : BaseEventArgs
    {
        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonProperty(PropertyName = "guild_id")]
        internal long GuildId { get; set; }

        /// <summary>
        /// 频道ID
        /// </summary>
        [JsonProperty(PropertyName = "channel_id")]
        internal long ChannelId { get; set; }

        /// <summary>
        /// 子频道ID
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        internal long UserId { get; set; }

        /// <summary>
        /// <para>操作者/发送者用户ID</para>
        /// <para>在频道系统中代表用户ID</para>
        /// <para>与QQ号并不通用</para>
        /// </summary>
        [JsonProperty(PropertyName = "self_tiny_id")]
        internal long SelfTinyId { get; set; }
    }
}
