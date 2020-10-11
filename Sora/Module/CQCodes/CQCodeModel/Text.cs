using Newtonsoft.Json;

namespace Sora.Module.CQCodes.CQCodeModel
{
    /// <summary>
    /// 纯文本
    /// </summary>
    internal struct Text
    {
        #region 属性
        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        internal string Content { get; set; }
        #endregion
    }
}
