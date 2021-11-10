using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 精华消息变动事件
/// </summary>
internal class OnebotEssenceChangeEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    internal long GroupId { get; set; }

    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "message_id")]
    internal long MessageId { get; set; }

    /// <summary>
    /// 操作者ID
    /// </summary>
    [JsonProperty(PropertyName = "operator_id")]
    internal long OperatorId { get; set; }

    /// <summary>
    /// 发送者ID
    /// </summary>
    [JsonProperty(PropertyName = "sender_id")]
    internal long SenderId { get; set; }

    /// <summary>
    /// 事件子类型
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "sub_type")]
    internal EssenceChangeType EssenceChangeType { get; set; }
}