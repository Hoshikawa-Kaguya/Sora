using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群管理员变动事件
    /// </summary>
    internal sealed class AdminChangeEventArgs : BaseNoticeEventArgs
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
    }
}
