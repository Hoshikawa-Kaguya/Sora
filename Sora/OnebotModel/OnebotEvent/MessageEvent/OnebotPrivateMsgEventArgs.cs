using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.OnebotEvent.MessageEvent;

/// <summary>
/// 私聊消息事件
/// </summary>
internal sealed class OnebotPrivateMsgEventArgs : BaseMessageEventArgs
{
    /// <summary>
    /// 发送人信息
    /// </summary>
    [JsonProperty(PropertyName = "sender")]
    internal PrivateSenderInfo SenderInfo { get; set; }
}