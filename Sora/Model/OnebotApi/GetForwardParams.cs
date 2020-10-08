using Newtonsoft.Json;

namespace Sora.Model.OnebotApi
{
    /// <summary>
    /// 获取合并转发消息
    /// </summary>
    internal struct GetForwardParams
    {
        /// <summary>
        /// 消息id
        /// </summary>
        [JsonProperty(PropertyName = "message_id")]
        internal string MessageId { get; set; }
    }
}
