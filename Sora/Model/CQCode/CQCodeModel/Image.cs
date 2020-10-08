using Newtonsoft.Json;
using Sora.Converter;

namespace Sora.Model.CQCode.CQCodeModel
{
    public class Image
    {
        #region 属性
        /// <summary>
        /// 纯文本内容
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        internal string ImgFile { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        internal string ImgType { get; set; }

        /// <summary>
        /// 图片链接
        /// </summary>
        [JsonProperty(PropertyName = "url", NullValueHandling = NullValueHandling.Ignore)]
        internal string Url { get; set; }

        /// <summary>
        /// 只在通过网络 URL 发送时有效，表示是否使用已缓存的文件
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "cache", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Cache { get; set; }

        /// <summary>
        /// 只在通过网络 URL 发送时有效，表示是否通过代理下载文件
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "proxy", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Proxy { get; set; } 

        /// <summary>
        /// 只在通过网络 URL 发送时有效（s）
        /// </summary>
        [JsonConverter(typeof(StringConverter))]
        [JsonProperty(PropertyName = "timeout", NullValueHandling = NullValueHandling.Ignore)]
        internal int? Timeout { get; set; }
        #endregion

        #region 构造函数(仅用于JSON消息段构建)
        internal Image(){}
        #endregion
    }
}
