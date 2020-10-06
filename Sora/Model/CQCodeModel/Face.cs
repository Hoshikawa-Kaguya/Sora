using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodeModel
{
    /// <summary>
    /// QQ 表情
    /// </summary>
    public class Face
    {
        #region 属性
        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        internal int Id { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Face() {}
        #endregion
    }
}
