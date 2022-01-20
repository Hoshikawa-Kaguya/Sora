using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 离线文件事件
/// </summary>
internal sealed class OnebotOfflineFileEventArgs : BaseObNoticeEventArgs
{
    /// <summary>
    /// 离线文件信息
    /// </summary>
    [JsonProperty(PropertyName = "file")]
    internal OfflineFileInfo Info { get; set; }
}