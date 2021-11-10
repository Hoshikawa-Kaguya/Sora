using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info;

/// <summary>
/// 私聊消息发送者
/// </summary>
public struct PrivateSenderInfo
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
    /// <para>来源群号</para>
    /// <para>仅支持GoCQ</para>
    /// </summary>
    [JsonProperty(PropertyName = "group_id", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    public long? GroupId { get; internal init; }

    /// <summary>
    /// 权限等级
    /// </summary>
    [JsonIgnore]
    public MemberRoleType Role { get; internal set; }
}