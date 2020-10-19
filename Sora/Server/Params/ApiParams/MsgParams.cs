using Newtonsoft.Json;

namespace Sora.Server.Params.ApiParams
{
    /// <summary>
    /// 撤回消息
    /// </summary>
    internal struct MsgParams
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonProperty(PropertyName = "message_id")]
        internal int MessageId { get; set; }
    }
}
