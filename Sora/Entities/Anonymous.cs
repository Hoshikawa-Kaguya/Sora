using System;
using Newtonsoft.Json;

namespace Sora.Entities;

/// <summary>
/// 匿名用户类
/// </summary>
public sealed class Anonymous
{
    #region 属性

    /// <summary>
    /// 匿名用户 flag
    /// </summary>
    [JsonProperty(PropertyName = "flag")]
    public string Flag { get; private init; }

    /// <summary>
    /// 匿名用户 ID
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public long Id { get; private init; }

    /// <summary>
    /// 匿名用户名称
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string Name { get; private init; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    internal Anonymous()
    {
    }

    #endregion

    #region 运算符重载

    /// <summary>
    /// 等于重载
    /// </summary>
    public static bool operator ==(Anonymous anonymousL, Anonymous anonymousR)
    {
        if (anonymousL is null && anonymousR is null) return true;

        return anonymousL is not null                  && anonymousR is not null &&
               anonymousL.Flag.Equals(anonymousR.Flag) &&
               anonymousL.Id == anonymousR.Id          &&
               anonymousL.Name.Equals(anonymousR.Name);
    }

    /// <summary>
    /// 不等于重载
    /// </summary>
    public static bool operator !=(Anonymous anonymousL, Anonymous anonymousR)
    {
        return !(anonymousL == anonymousR);
    }

    #endregion

    #region 常用重载

    /// <summary>
    /// 比较重载
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is Anonymous anonymous) return this == anonymous;

        return false;
    }

    /// <summary>
    /// GetHashCode
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Flag, Id, Name);
    }

    #endregion
}