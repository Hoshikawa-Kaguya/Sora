using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// At某人
    /// </summary>
    internal struct At
    {
        #region 属性
        /// <summary>
        /// At目标UID
        /// 为<see langword="null"/>时为At全体
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "qq")]
        internal string Traget { get; set; }
        #endregion
    }
}
