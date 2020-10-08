using Newtonsoft.Json;
using Sora.Model.Message;

namespace Sora.EventArgs.OnebotEvent.MessageEvent
{
    /// <summary>
    /// 私聊消息事件
    /// </summary>
    internal sealed class PrivateMessageEventArgs : BaseMessageEventArgs
    {
        /// <summary>
        /// 发送人信息
        /// </summary>
        [JsonProperty(PropertyName = "sender")]
        internal PrivateSender Sender { get; set; }
    }
}
