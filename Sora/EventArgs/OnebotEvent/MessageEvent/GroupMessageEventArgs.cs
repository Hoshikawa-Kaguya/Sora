using Newtonsoft.Json;
using Sora.Model.Message;

namespace Sora.EventArgs.OnebotEvent.MessageEvent
{
    /// <summary>
    /// 群组消息事件
    /// </summary>
    internal sealed class GroupMessageEventArgs : BaseMessageEventArgs
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long GroupId { get; set; }

        /// <summary>
        /// 匿名信息
        /// </summary>
        [JsonProperty(PropertyName = "anonymous")]
        internal object Anonymous { get; set; }

        /// <summary>
        /// 发送人信息
        /// </summary>
        [JsonProperty(PropertyName = "sender")]
        internal GroupSender Sender { get; set; }
    }
}
