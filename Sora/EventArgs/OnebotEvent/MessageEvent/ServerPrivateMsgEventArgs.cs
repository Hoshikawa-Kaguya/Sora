using Newtonsoft.Json;
using Sora.Module.Info;

namespace Sora.EventArgs.OnebotEvent.MessageEvent
{
    /// <summary>
    /// 私聊消息事件
    /// </summary>
    internal sealed class ServerPrivateMsgEventArgs : BaseMessageEventArgs
    {
        /// <summary>
        /// 发送人信息
        /// </summary>
        [JsonProperty(PropertyName = "sender")]
        internal PrivateSenderInfo SenderInfo { get; set; }
    }
}
