using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.MetaEvent
{
    internal sealed class LifeCycleEventArgs : BaseMetaEventArgs
    {
        /// <summary>
        /// <para>事件子类型</para>
        /// <para>只可能为<see langword="connect"/></para>
        /// </summary>
        [JsonProperty(PropertyName = "sub_type")]
        public string SubType { get; set; }
    }
}
