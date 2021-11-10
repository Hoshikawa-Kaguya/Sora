using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.OnebotEvent.NoticeEvent;

/// <summary>
/// 其他客户端在线状态变更
/// </summary>
internal sealed class OnebotClientStatusEventArgs : BaseNoticeEventArgs
{
    /// <summary>
    /// 客户端信息
    /// </summary>
    [JsonProperty(PropertyName = "client")]
    internal ClientInfo ClientInfo { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    [JsonProperty(PropertyName = "online")]
    internal bool Online { get; set; }
}