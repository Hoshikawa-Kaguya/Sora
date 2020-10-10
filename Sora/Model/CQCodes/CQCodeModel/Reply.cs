using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 回复
    /// </summary>
    internal struct Reply
    {
        #region 属性
        /// <summary>
        /// At目标UID
        /// 为<see langword="null"/>时为At全体
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        internal int Traget { get; set; }
        #endregion
    }
}
