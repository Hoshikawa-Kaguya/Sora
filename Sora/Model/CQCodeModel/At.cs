using Newtonsoft.Json;
using Sora.Converter;
using System;

namespace Sora.Model.CQCodeModel
{
    /// <summary>
    /// At某人
    /// </summary>
    public class At
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

        #region 构造函数(仅用于JSON消息段构建)
        internal At(){}
        #endregion
    }
}
