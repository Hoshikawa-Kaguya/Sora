using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Entities.Info;

namespace Sora.OnebotModel.ExtraEvent;

/// <summary>
/// 频道消息表情贴更新
/// </summary>
internal sealed class GocqReactionsUpdatedEventArgs : BaseGuildNoticeEventArgs
{
    /// <summary>
    /// <para>消息ID</para>
    /// </summary>
    [JsonProperty(PropertyName = "message_id")]
    internal int MessageId { get; set; }

    /// <summary>
    /// TODO 作用不明
    /// </summary>
    [JsonProperty(PropertyName = "message_sender_uin")]
    internal ulong MessageSenderId { get; set; }

    /// <summary>
    /// 当前消息被贴表情列表
    /// </summary>
    [JsonProperty(PropertyName = "current_reactions")]
    internal List<ReactionInfo> Reactions { get; set; }
}