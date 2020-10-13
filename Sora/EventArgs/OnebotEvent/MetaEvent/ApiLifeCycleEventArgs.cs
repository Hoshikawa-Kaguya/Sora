using Newtonsoft.Json;

namespace Sora.EventArgs.OnebotEvent.MetaEvent
{
    /// <summary>
    /// 生命周期事件
    /// </summary>
    internal sealed class ApiLifeCycleEventArgs : BaseMetaEventArgs
    {
        /// <summary>
        /// <para>事件子类型</para>
        /// <para>只可能为<see langword="connect"/></para>
        /// </summary>
        [JsonProperty(PropertyName = "sub_type")]
        internal string SubType { get; set; }
    }
}
