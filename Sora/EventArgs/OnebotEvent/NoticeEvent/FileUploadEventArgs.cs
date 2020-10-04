using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群文件上传事件
    /// </summary>
    internal sealed class FileUploadEventArgs : BaseNoticeEventArgs
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long GroupId { get; set; }

        /// <summary>
        /// 上传的文件信息
        /// </summary>
        [JsonProperty(PropertyName = "file")]
        internal UploadFile Upload { get; set; }
    }

    internal sealed class UploadFile
    {
        /// <summary>
        /// 文件 ID
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        internal string FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        internal string Name { get; set; }

        /// <summary>
        /// 文件大小(Byte)
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        internal long Size { get; set; }

        /// <summary>
        /// 未知字段
        /// </summary>
        [JsonProperty(PropertyName = "busid")]
        internal long Busid { get; set; }
    }
}
