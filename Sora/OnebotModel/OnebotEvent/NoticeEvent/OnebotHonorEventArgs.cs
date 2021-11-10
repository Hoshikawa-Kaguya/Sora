using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 群成员荣誉变更事件
/// </summary>
internal sealed class OnebotHonorEventArgs : BaseNotifyEventArgs
{
    /// <summary>
    /// 荣誉类型
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "honor_type")]
    internal HonorType HonorType { get; set; }
}