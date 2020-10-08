using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// <para>Xml与Json集合</para>
    /// <para>可能为<see cref="CQFunction"/>.<see langword="Json"/>或<see cref="CQFunction"/>.<see langword="Xml"/></para>
    /// </summary>
    internal class Code
    {
        #region 属性
        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        internal string Content { get; set; }

        /// <summary>
        /// 是否走富文本通道
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "resid",NullValueHandling = NullValueHandling.Ignore)]
        internal int? Resid { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Code() {}
        #endregion
    }
}
