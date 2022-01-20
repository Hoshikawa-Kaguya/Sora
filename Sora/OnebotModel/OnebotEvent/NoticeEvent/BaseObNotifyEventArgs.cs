using Newtonsoft.Json;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 群内通知类事件
/// </summary>
internal abstract class BaseObNotifyEventArgs : BaseObNoticeEventArgs
{
    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    internal long GroupId { get; set; }
}