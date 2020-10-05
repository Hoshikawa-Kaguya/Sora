using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.MetaEvent
{
    /// <summary>
    /// 元事件基类
    /// </summary>
    internal abstract class BaseMetaEventArgs : OnebotBaseEventArgs
    {
        /// <summary>
        /// 元事件类型
        /// </summary>
        [JsonProperty(PropertyName = "meta_event_type")]
        internal string MetaEventType { get; set; }
    }
}
