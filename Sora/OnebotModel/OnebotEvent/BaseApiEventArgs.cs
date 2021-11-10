using Newtonsoft.Json;

namespace Sora.OnebotModel.OnebotEvent;

/// <summary>
/// OneBot事件基类
/// </summary>
internal abstract class BaseApiEventArgs
{
    /// <summary>
    /// 事件发生的时间戳
    /// </summary>
    [JsonProperty(PropertyName = "time", NullValueHandling = NullValueHandling.Ignore)]
    internal long Time { get; set; }

    /// <summary>
    /// 收到事件的机器人 QQ 号
    /// </summary>
    [JsonProperty(PropertyName = "self_id", NullValueHandling = NullValueHandling.Ignore)]
    internal long SelfID { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    [JsonProperty(PropertyName = "post_type", NullValueHandling = NullValueHandling.Ignore)]
    internal string PostType { get; set; }
}