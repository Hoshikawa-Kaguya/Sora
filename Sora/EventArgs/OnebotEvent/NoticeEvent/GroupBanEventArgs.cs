using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群禁言事件
    /// </summary>
    internal sealed class GroupBanEventArgs : BaseNoticeEventArgs
    {
        /// <summary>
        /// 事件子类型
        /// </summary>
        [JsonProperty(PropertyName = "sub_type")]
        internal string SubType { get; set; }

        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long GroupId { get; set; }

        /// <summary>
        /// 操作者 UID
        /// </summary>
        [JsonProperty(PropertyName = "operator_id")]
        internal long OperatorId { get; set; }

        /// <summary>
        /// 禁言时长(s)
        /// </summary>
        [JsonProperty(PropertyName = "duration")]
        internal long Duration { get; set; }
    }
}
