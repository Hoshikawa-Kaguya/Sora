using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 好友消息撤回
    /// </summary>
    internal sealed class FriendRecallEventArgs : BaseNoticeEventArgs
    {
        /// <summary>
        /// 被撤回的消息 ID
        /// </summary>
        [JsonProperty(PropertyName = "message_id")]
        internal long MessageId { get; set; }
    }
}
