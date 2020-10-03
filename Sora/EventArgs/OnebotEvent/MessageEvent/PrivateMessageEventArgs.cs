using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.EventArgs.OnebotEvent.MessageEvent
{
    internal sealed class PrivateMessageEventArgs : BaseMessageEventArgs
    {
        /// <summary>
        /// 反序列化Sender
        /// </summary>
        internal void ParseSender()
        {
            if(base.Sender == null) return;
            base.Sender = ((JObject) base.Sender).ToObject<PrivateSender>();
        }

        /// <summary>
        /// 获取发送者信息
        /// </summary>
        internal PrivateSender GetSender() => (PrivateSender) base.Sender;
    }

    /// <summary>
    /// 私聊信息发送者
    /// </summary>
    internal class PrivateSender
    {
        /// <summary>
        /// 发送者 QQ 号
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        internal long UserId { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        [JsonProperty(PropertyName = "nickname")]
        internal string Nick { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [JsonProperty(PropertyName = "sex")]
        internal string Sex { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [JsonProperty(PropertyName = "age")]
        internal int Age { get; set; }
    }
}
