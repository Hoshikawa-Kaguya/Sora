using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群内通知类事件
    /// </summary>
    internal class BaseNotifyEventArgs : BaseNoticeEventArgs
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long GroupId { get; set; }
    }
}
