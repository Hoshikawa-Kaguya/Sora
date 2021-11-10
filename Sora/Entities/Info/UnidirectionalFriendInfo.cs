using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 单向好友信息
/// </summary>
public readonly struct UnidirectionalFriendInfo
{
    /// <summary>
    /// 昵称
    /// </summary>
    [JsonProperty(PropertyName = "nickname")]
    public string NickName { get; internal init; }

    /// <summary>
    /// 用户QQ号
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    public long UserId { get; internal init; }

    /// <summary>
    /// 添加途径
    /// </summary>
    [JsonProperty(PropertyName = "source")]
    public string Source { get; internal init; }
}