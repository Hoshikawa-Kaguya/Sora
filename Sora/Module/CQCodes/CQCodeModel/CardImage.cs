using Newtonsoft.Json;

namespace Sora.Module.CQCodes.CQCodeModel
{
    /// <summary>
    /// 装逼大图
    /// 仅支持Go
    /// </summary>
    public struct CardImage
    {
        /// <summary>
        /// 和image相同
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        public string ImageFile { get; internal set; }

        /// <summary>
        /// 最小width
        /// </summary>
        [JsonProperty(PropertyName = "minwidth")]
        public long MinWidth { get; internal set; }

        /// <summary>
        /// 最小height
        /// </summary>
        [JsonProperty(PropertyName = "minheight")]
        public long MinHeight { get; internal set; }

        /// <summary>
        /// 最大width
        /// </summary>
        [JsonProperty(PropertyName = "maxwidth")]
        public long MaxWidth { get; internal set; }

        /// <summary>
        /// 最大height
        /// </summary>
        [JsonProperty(PropertyName = "maxheight")]
        public long MaxHeight { get; internal set; }

        /// <summary>
        /// 来源名称
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public string Source { get; internal set; }

        /// <summary>
        /// 来源图标url
        /// </summary>
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; internal set; }
    }
}
