using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 频道成员信息
/// </summary>
public readonly struct GuildMemberInfo
{
    /// <summary>
    /// 成员ID
    /// </summary>
    [JsonProperty(PropertyName = "tiny_id")]
    public ulong UserGuildId { get; internal init; }

    /// <summary>
    /// 成员头衔
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    public string Title { get; internal init; }

    /// <summary>
    /// 成员昵称
    /// </summary>
    [JsonProperty(PropertyName = "nickname")]
    public string UserGuildNick { get; internal init; }

    /// <summary>
    /// 成员权限类型
    /// </summary>
    [JsonProperty(PropertyName = "role")]
    public int UserGuildRole { get; internal init; }
}