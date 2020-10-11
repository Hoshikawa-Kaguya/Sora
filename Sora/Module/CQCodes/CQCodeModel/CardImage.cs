using Newtonsoft.Json;

namespace Sora.Module.CQCodes.CQCodeModel
{
    /// <summary>
    /// 装逼大图
    /// 仅支持Go
    /// </summary>
    internal struct CardImage
    {
        /// <summary>
        /// 和image相同
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        internal string ImageFile { get; set; }

        /// <summary>
        /// 最小width
        /// </summary>
        [JsonProperty(PropertyName = "minwidth")]
        internal long MinWidth { get; set; }

        /// <summary>
        /// 最小height
        /// </summary>
        [JsonProperty(PropertyName = "minheight")]
        internal long MinHeight { get; set; }

        /// <summary>
        /// 最大width
        /// </summary>
        [JsonProperty(PropertyName = "maxwidth")]
        internal long MaxWidth { get; set; }

        /// <summary>
        /// 最大height
        /// </summary>
        [JsonProperty(PropertyName = "maxheight")]
        internal long MaxHeight { get; set; }

        /// <summary>
        /// 来源名称
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        internal string Source { get; set; }

        /// <summary>
        /// 来源图标url
        /// </summary>
        [JsonProperty(PropertyName = "icon")]
        internal string Icon { get; set; }
    }
}
