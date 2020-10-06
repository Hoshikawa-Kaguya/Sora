using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.RequestEvent
{
    /// <summary>
    /// 请求事件基类
    /// </summary>
    internal abstract class BaseRequestEvent : BaseOnebotEventArgs
    {
        /// <summary>
        /// 请求类型
        /// </summary>
        [JsonProperty(PropertyName = "request_type")]
        internal string RequestType { get; set; }

        /// <summary>
        /// 发送请求的 QQ 号
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        internal long UserId { get; set; }

        /// <summary>
        /// 验证信息
        /// </summary>
        [JsonProperty(PropertyName = "comment")]
        internal string Comment { get; set; }

        /// <summary>
        /// 请求 flag
        /// </summary>
        [JsonProperty(PropertyName = "flag")]
        internal string Flag { get; set; }
    }
}
