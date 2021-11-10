using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info;

/// <summary>
/// 群组消息发送者
/// </summary>
public struct GroupSenderInfo
{
    /// <summary>
    /// 发送者 QQ 号
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    public long UserId { get; internal init; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonProperty(PropertyName = "nickname")]
    public string Nick { get; internal init; }

    /// <summary>
    /// 群名片/备注
    /// </summary>
    [JsonProperty(PropertyName = "card")]
    public string Card { get; internal init; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonProperty(PropertyName = "sex")]
    [JsonConverter(typeof(EnumDescriptionConverter))]
    public Sex Sex { get; internal init; }

    /// <summary>
    /// 年龄
    /// </summary>
    [JsonProperty(PropertyName = "age")]
    public int Age { get; internal init; }

    /// <summary>
    /// 地区
    /// </summary>
    [JsonProperty(PropertyName = "area")]
    public string Area { get; internal init; }

    /// <summary>
    /// 成员等级
    /// </summary>
    [JsonProperty(PropertyName = "level")]
    public string Level { get; internal init; }

    /// <summary>
    /// 角色(权限等级)
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "role")]
    public MemberRoleType Role { get; internal set; }

    /// <summary>
    /// 专属头衔
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    public string Title { get; internal init; }
}