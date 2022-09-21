using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 好友信息
/// </summary>
public sealed record FriendInfo
{
#region 属性

    /// <summary>
    /// 好友备注
    /// </summary>
    [JsonProperty(PropertyName = "remark", NullValueHandling = NullValueHandling.Ignore)]
    public string Remark { get; internal init; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonProperty(PropertyName = "nickname", NullValueHandling = NullValueHandling.Ignore)]
    public string Nick { get; internal init; }

    /// <summary>
    /// 是否为机器人管理员
    /// </summary>
    [JsonIgnore]
    public bool IsSuperUser { get; internal set; }

    /// <summary>
    /// 好友ID
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    public long UserId { get; internal init; }

#endregion
}