using Newtonsoft.Json;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 合并转发/合并转发节点
    /// </summary>
    public class ForwardSegment : BaseSegment
    {
        #region 属性

        /// <summary>
        /// 转发消息ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string MessageId { get; internal set; }

        #endregion
    }
}