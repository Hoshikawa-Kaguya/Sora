using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// QQ 表情
    /// </summary>
    internal struct Face
    {
        #region 属性
        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        internal int Id { get; set; }
        #endregion
    }
}
