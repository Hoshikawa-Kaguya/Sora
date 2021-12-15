using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.ExtraEvent;

/// <summary>
/// 子频道信息更新
/// </summary>
internal class GocqChannelInfoUpdateEventArgs : BaseGuildNoticeEventArgs
{
    /// <summary>
    /// 更新前的频道信息
    /// </summary>
    [JsonProperty(PropertyName = "old_info")]
    internal ChannelInfo OldInfo { get; set; }

    /// <summary>
    /// 更新后的频道信息
    /// </summary>
    [JsonProperty(PropertyName = "new_info")]
    internal ChannelInfo NewInfo { get; set; }
}