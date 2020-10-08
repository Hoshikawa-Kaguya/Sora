using Newtonsoft.Json;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 纯文本
    /// </summary>
    internal class Text
    {
        #region 属性
        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        internal string Content { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Text(){}
        #endregion
    }
}
