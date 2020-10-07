using System;
using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodeModel
{
    /// <summary>
    /// 短视频
    /// </summary>
    [Obsolete]
    internal class Video
    {
        #region 属性
        /// <summary>
        /// 视频文件名
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        internal string VideoFile { get; set; }

        /// <summary>
        /// 视频 URL
        /// </summary>
        [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
        internal string Url { get; set; }

        /// <summary>
        /// 是否使用已缓存的文件
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Cache { get; set; }

        /// <summary>
        /// 是否使用已缓存的文件
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "proxy", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Proxy { get; set; }

        /// <summary>
        /// 是否使用已缓存的文件
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "timeout", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Timeout { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Video(){}
        #endregion
    }
}
