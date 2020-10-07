using Newtonsoft.Json;
using Sora.Enumeration;

namespace Sora.Model.CQCodeModel
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
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Code() {}
        #endregion
    }
}
