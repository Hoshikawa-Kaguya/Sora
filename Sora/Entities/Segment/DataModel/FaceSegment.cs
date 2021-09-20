using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel
{
    /// <summary>
    /// QQ 表情
    /// </summary>
    public class FaceSegment : BaseSegment
    {
        #region 属性

        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        public int Id { get; internal set; }

        #endregion
    }
}