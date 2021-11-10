using Newtonsoft.Json;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 群成员头衔变更事件
/// </summary>
internal sealed class OnebotMemberTitleUpdatedEventArgs : BaseNotifyEventArgs
{
    /// <summary>
    /// 新头衔
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    internal string NewTitle { get; set; }
}