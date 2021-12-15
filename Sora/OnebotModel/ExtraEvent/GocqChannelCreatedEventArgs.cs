using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.ExtraEvent;

/// <summary>
/// 子频道创建/删除
/// </summary>
internal class GocqChannelCreatedOrDestroyedEventArgs : BaseGuildNoticeEventArgs
{
    /// <summary>
    /// 频道信息
    /// </summary>
    [JsonProperty(PropertyName = "channel_info")]
    internal ChannelInfo ChannelInfo { get; set; }
}