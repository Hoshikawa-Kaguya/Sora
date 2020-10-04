using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群成员变动事件
    /// </summary>
    internal sealed class GroupMemberChangeEventArgs : BaseNoticeEventArgs
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
        /// 操作者 QQ 号
        /// </summary>
        [JsonProperty(PropertyName = "operator_id")]
        internal long OperatorId { get; set; }
    }
}
