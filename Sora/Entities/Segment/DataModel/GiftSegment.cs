using Newtonsoft.Json;

namespace Sora.Entities.Segment.DataModel
{
    /// <summary>
    /// 礼物
    /// 仅支持GoCQ
    /// </summary>
    public class GiftSegment : BaseSegment
    {
        /// <summary>
        /// 接收目标
        /// </summary>
        [JsonProperty(PropertyName = "qq")]
        public long Target { get; internal set; }

        /// <summary>
        /// 礼物类型
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int GiftType { get; internal set; }
    }
}