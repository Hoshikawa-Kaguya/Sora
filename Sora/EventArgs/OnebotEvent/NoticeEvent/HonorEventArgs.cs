using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.NoticeEvent
{
    /// <summary>
    /// 群成员荣誉变更事件
    /// </summary>
    internal sealed class HonorEventArgs : BaseNotifyEventArgs
    {
        /// <summary>
        /// 荣誉类型
        /// </summary>
        [JsonProperty(PropertyName = "honor_type")]
        internal string HonorType { get; set; }
    }
}
