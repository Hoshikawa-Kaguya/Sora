using Newtonsoft.Json;

namespace Sora.Module.CQCodes.CQCodeModel
{
    /// <summary>
    /// 礼物
    /// 仅支持Go
    /// </summary>
    internal struct Gift
    {
        /// <summary>
        /// 接收目标
        /// </summary>
        [JsonProperty(PropertyName = "qq")]
        internal long Target { get; set; }

        /// <summary>
        /// 礼物类型
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        internal int GiftType { get; set; }
    }
}
