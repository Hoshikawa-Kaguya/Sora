using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Params.ApiParams
{
    /// <summary>
    /// 撤回消息
    /// </summary>
    internal struct MsgParams
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "message_id")]
        internal int MessageId { get; set; }
    }
}
