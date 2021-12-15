using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sora.OnebotModel.ExtraEvent;

/// <summary>
/// <para>频道通知事件鸡肋</para>
/// <para>TODO 也许v12能合并到ob的事件类型里把（真的很乱）</para>
/// </summary>
internal abstract class BaseGuildNoticeEventArgs : BaseGuildEventArgs
{
    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonProperty(PropertyName = "notice_type", NullValueHandling = NullValueHandling.Ignore)]
    internal string NoticeType { get; set; }

    /// <summary>
    /// 操作对象UID
    /// </summary>
    [JsonProperty(PropertyName = "user_id", NullValueHandling = NullValueHandling.Ignore)]
    internal long UserId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonProperty(PropertyName = "operator_id", NullValueHandling = NullValueHandling.Ignore)]
    internal ulong OperatorId { get; set; }
}