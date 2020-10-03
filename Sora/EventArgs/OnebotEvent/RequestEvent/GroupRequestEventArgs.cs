using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.RequestEvent
{
    /// <summary>
    /// 群聊邀请/入群请求事件
    /// </summary>
    internal class GroupRequestEventArgs : BaseRequestEvent
    {
        /// <summary>
        /// 请求子类型
        /// </summary>
        [JsonProperty(PropertyName = "sub_type")]
        internal string SubType { get; set; }

        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long GroupId { get; set; }
    }
}
