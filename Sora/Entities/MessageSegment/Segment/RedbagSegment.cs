using Newtonsoft.Json;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 接收红包
    /// 仅支持Go
    /// </summary>
    public class RedbagSegment : BaseSegment
    {
        /// <summary>
        /// 祝福语/口令
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; internal set; }
    }
}