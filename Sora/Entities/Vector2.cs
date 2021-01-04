using Newtonsoft.Json;

namespace Sora.Entities
{
    /// <summary>
    /// 二维向量
    /// </summary>
    public class Vector2
    {
        #region 属性
        /// <summary>
        /// X
        /// </summary>
        [JsonProperty(PropertyName = "x")]
        public int X { get; internal set; }

        /// <summary>
        /// Y
        /// </summary>
        [JsonProperty(PropertyName = "y")]
        public int Y { get; internal set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        internal Vector2() {}
        #endregion
    }
}
