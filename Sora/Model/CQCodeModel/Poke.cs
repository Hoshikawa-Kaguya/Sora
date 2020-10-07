using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodeModel
{
    /// <summary>
    /// <para>群成员戳一戳</para>
    /// <para>仅发送</para>
    /// </summary>
    internal class Poke
    {
        #region 属性
        /// <summary>
        /// 需要戳的成员
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "qq")]
        internal long Uid { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Poke(){}
        #endregion
    }
}
