using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodeModel
{
    /// <summary>
    /// 回复
    /// </summary>
    internal class Reply
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

        #region 构造函数(仅用于JSON消息段构建)
        internal Reply(){}
        #endregion
    }
}
