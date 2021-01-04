using Newtonsoft.Json;

namespace Sora.Entities
{
    /// <summary>
    /// 匿名用户实例
    /// </summary>
    public class Anonymous
    {
        #region 属性
        /// <summary>
        /// 匿名用户 flag
        /// </summary>
        [JsonProperty(PropertyName = "flag")]
        public string Flag { get; internal set; }

        /// <summary>
        /// 匿名用户 ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public long Id { get; internal set; }

        /// <summary>
        /// 匿名用户名称
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; internal set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        internal Anonymous() {}
        #endregion
    }
}
