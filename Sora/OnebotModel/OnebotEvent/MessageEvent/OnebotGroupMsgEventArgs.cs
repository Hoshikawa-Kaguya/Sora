using Newtonsoft.Json;
using Sora.Entities;
using Sora.Entities.Info;

namespace Sora.OnebotModel.OnebotEvent.MessageEvent;

/// <summary>
/// 群组消息事件
/// </summary>
internal sealed class OnebotGroupMsgEventArgs : BaseObMessageEventArgs
{
    /// <summary>
    /// 群号
    /// </summary>
    [JsonProperty(PropertyName = "group_id")]
    internal long GroupId { get; set; }

    /// <summary>
    /// 匿名信息
    /// </summary>
    [JsonProperty(PropertyName = "anonymous", NullValueHandling = NullValueHandling.Ignore)]
    internal Anonymous Anonymous { get; set; }

    /// <summary>
    /// 发送人信息
    /// </summary>
    [JsonProperty(PropertyName = "sender")]
    internal GroupSenderInfo SenderInfo { get; set; }

    /// <summary>
    /// 消息序号
    /// </summary>
    [JsonProperty(PropertyName = "message_seq")]
    internal long MessageSequence { get; set; }
}