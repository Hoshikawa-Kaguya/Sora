using Newtonsoft.Json;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 纯文本
    /// </summary>
    public class TextSegment : BaseSegment
    {
        #region 属性

        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Content { get; internal set; }

        #endregion
    }
}