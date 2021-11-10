using Newtonsoft.Json;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info;

/// <summary>
/// 用户信息
/// </summary>
public struct UserInfo
{
    #region 属性

    /// <summary>
    /// 用户id
    /// </summary>
    [JsonProperty(PropertyName = "user_id")]
    public long UserId { get; internal init; }

    /// <summary>
    /// 权限等级
    /// </summary>
    [JsonIgnore]
    public MemberRoleType Role { get; internal set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonProperty(PropertyName = "nickname")]
    public string Nick { get; internal init; }

    /// <summary>
    /// 年龄
    /// </summary>
    [JsonProperty(PropertyName = "age")]
    public int Age { get; internal init; }

    /// <summary>
    /// 性别
    /// </summary>
    [JsonProperty(PropertyName = "sex")]
    public string Sex { get; internal init; }

    /// <summary>
    /// 等级
    /// </summary>
    [JsonProperty(PropertyName = "level")]
    public int Level { get; internal init; }

    /// <summary>
    /// 登陆天数
    /// </summary>
    [JsonProperty(PropertyName = "login_days")]
    public int LoginDays { get; internal init; }

    #endregion
}