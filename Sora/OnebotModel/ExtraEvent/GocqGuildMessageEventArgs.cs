using Newtonsoft.Json;

namespace Sora.OnebotModel.ExtraEvent
{
    internal sealed class GocqGuildMessageEventArgs : BaseGuildEventArgs
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonProperty(PropertyName = "message_id")]
        internal string MessageId { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        [JsonProperty(PropertyName = "message_type")]
        internal string MessageType { get; set; }

        /// <summary>
        /// 消息子类型
        /// </summary>
        [JsonProperty(PropertyName = "sub_type")]
        internal string SubType { get; set; }
    }
}
