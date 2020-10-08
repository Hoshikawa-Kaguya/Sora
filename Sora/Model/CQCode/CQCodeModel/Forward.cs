using Newtonsoft.Json;

namespace Sora.Model.CQCode.CQCodeModel
{
    /// <summary>
    /// 合并转发/合并转发节点
    /// </summary>
    internal class Forward
    {
        #region 属性
        /// <summary>
        /// 转发消息ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        internal string MessageId { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Forward() {}
        #endregion
    }
}
