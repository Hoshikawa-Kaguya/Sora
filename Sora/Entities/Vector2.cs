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
        public int X { get; set; }

        /// <summary>
        /// Y
        /// </summary>
        [JsonProperty(PropertyName = "y")]
        public int Y { get; set; }
        #endregion
    }
}
