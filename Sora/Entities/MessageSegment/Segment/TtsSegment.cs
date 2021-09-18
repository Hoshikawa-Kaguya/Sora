using Newtonsoft.Json;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 语音转文字（TTS）
    /// </summary>
    public class TtsSegment : BaseSegment
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