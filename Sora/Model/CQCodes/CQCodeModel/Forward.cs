using Newtonsoft.Json;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 合并转发/合并转发节点
    /// </summary>
    internal struct Forward
    {
        #region 属性
        /// <summary>
        /// 转发消息ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        internal string MessageId { get; set; }
        #endregion
    }
}
