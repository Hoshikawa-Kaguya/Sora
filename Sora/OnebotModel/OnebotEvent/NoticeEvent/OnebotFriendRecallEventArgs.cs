using Newtonsoft.Json;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 好友消息撤回
/// </summary>
internal sealed class OnebotFriendRecallEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 被撤回的消息 ID
    /// </summary>
    [JsonProperty(PropertyName = "message_id")]
    internal int MessageId { get; set; }
}