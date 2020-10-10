using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 音乐分享
    /// 仅发送
    /// </summary>
    internal struct Music
    {
        /// <summary>
        /// 音乐分享类型
        /// </summary>
        [JsonConverter(typeof(EnumDescriptionConverter))]
        [JsonProperty(PropertyName = "type")]
        internal MusicShareType MusicType { get; set; }

        /// <summary>
        /// 歌曲 ID
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "id")]
        internal long MusicId { get; set; }
    }
}
