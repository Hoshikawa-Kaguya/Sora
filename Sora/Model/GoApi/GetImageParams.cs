using Newtonsoft.Json;

namespace Sora.Model.GoApi
{
    /// <summary>
    /// 获取图片信息
    /// </summary>
    internal struct GetImageParams
    {
        [JsonProperty(PropertyName = "file")]
        internal string FileName { get; set; }
    }
}
