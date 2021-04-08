using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Entities.MessageElement.CQModel
{
    /// <summary>
    /// 回复
    /// </summary>
    public struct Reply
    {
        #region 属性

        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        public int Traget { get; internal set; }

        #endregion
    }
}