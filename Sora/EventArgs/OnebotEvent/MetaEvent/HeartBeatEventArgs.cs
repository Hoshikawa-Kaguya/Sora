using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.MetaEvent
{
    /// <summary>
    /// 心跳包
    /// </summary>
    internal sealed class HeartBeatEventArgs : BaseMetaEventArgs
    {
        /// <summary>
        /// 状态信息
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public object Status { get; set; }

        /// <summary>
        /// 到下次心跳的间隔，单位毫秒
        /// </summary>
        [JsonProperty(PropertyName = "interval")]
        public long Interval { get; set; }
    }
}
