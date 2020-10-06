using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCodeModel
{
    public class Record
    {
        #region 属性
        /// <summary>
        /// 语音文件名
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        internal string RecordFile { get; set; }

        /// <summary>
        /// 表示变声
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "magic")]
        internal int? Magic { get; set; }

        /// <summary>
        /// 语音 URL
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
    }
}
