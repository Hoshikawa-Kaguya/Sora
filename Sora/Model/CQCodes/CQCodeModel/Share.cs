using Newtonsoft.Json;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 链接分享
    /// </summary>
    internal struct Share
    {
        #region 属性
        /// <summary>
        /// URL
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        internal string Url { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        internal string Title { get; set; }

        /// <summary>
        /// 可选，内容描述
        /// </summary>
        [JsonProperty(PropertyName = "content",NullValueHandling = NullValueHandling.Ignore)]
        internal string Content { get; set; }

        /// <summary>
        /// 可选，图片 URL
        /// </summary>
        [JsonProperty(PropertyName = "image",NullValueHandling = NullValueHandling.Ignore)]
        internal string ImageUrl { get; set; }
        #endregion
    }
}
