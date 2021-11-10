using Newtonsoft.Json;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// GoCQ扩展事件
/// 群成员名片变更事件
/// </summary>
internal sealed class OnebotGroupCardUpdateEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    internal long GroupId { get; set; }

    /// <summary>
    /// 新名片
    /// </summary>
    [JsonProperty(PropertyName = "card_new")]
    internal string NewCard { get; set; }

    /// <summary>
    /// 旧名片
    /// </summary>
    [JsonProperty(PropertyName = "card_old")]
    internal string OldCard { get; set; }
}