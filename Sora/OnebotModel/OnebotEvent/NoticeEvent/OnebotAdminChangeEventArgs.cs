using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 群管理员变动事件
/// </summary>
internal sealed class OnebotAdminChangeEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 事件子类型
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "sub_type")]
    internal AdminChangeType SubType { get; set; }

    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    internal long GroupId { get; set; }
}