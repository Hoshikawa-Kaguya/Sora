using Newtonsoft.Json;

namespace Sora.Entities.CQCodes.CQCodeModel
{
    /// <summary>
    /// 图片
    /// </summary>
    public struct Image
    {
        #region 属性

        /// <summary>
        /// 文件名/绝对路径/URL/base64
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        public string ImgFile { get; internal set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        public string ImgType { get; internal set; }

        /// <summary>
        /// 图片链接
        /// </summary>
        [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; internal set; }

        /// <summary>
        /// 只在通过网络 URL 发送时有效，表示是否使用已缓存的文件
        /// </summary>
        [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
        public int? UseCache { get; internal set; }

        /// <summary>
        /// 通过网络下载图片时的线程数，默认单线程。
        /// </summary>
        [JsonProperty(PropertyName = "c", NullValueHandling = NullValueHandling.Ignore)]
        public int? ThreadCount { get; internal set; }

        /// <summary>
        /// 发送秀图时的特效id，默认为40000
        /// </summary>
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public int? Id { get; internal set; }

        #endregion
    }
}