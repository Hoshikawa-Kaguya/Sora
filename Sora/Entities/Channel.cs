using System;
using Sora.Entities.Base;

namespace Sora.Entities;

//TODO 完善相关方法
/// <summary>
/// 子频道实例
/// </summary>
public class Channel : BaseModel
{
    #region 属性

    /// <summary>
    /// 子频道ID
    /// </summary>
    public long Id { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器连接标识</param>
    /// <param name="cid">子频道ID</param>
    internal Channel(Guid serviceId, Guid connectionId, long cid) : base(serviceId, connectionId)
    {
        Id = cid;
    }

    #endregion

    #region 转换方法

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="long"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator long(Channel value)
    {
        return value.Id;
    }

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="string"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator string(Channel value)
    {
        return value.ToString();
    }

    #endregion
}