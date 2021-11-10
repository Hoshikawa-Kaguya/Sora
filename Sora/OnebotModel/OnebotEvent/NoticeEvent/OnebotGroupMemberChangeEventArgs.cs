using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 群成员变动事件
/// </summary>
internal sealed class OnebotGroupMemberChangeEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 事件子类型
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "sub_type")]
    internal MemberChangeType SubType { get; set; }

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