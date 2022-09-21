using System;
using System.Threading.Tasks;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 群戳一戳事件参数
/// </summary>
public sealed class GroupPokeEventArgs : BaseSoraEventArgs
{
#region 属性

    /// <summary>
    /// 发送者
    /// </summary>
    public User SendUser { get; }

    /// <summary>
    /// 被戳者
    /// </summary>
    public User TargetUser { get; }

    /// <summary>
    /// 消息源群
    /// </summary>
    public Group SourceGroup { get; }

#endregion 属性

#region 快捷方法

    /// <summary>
    /// 戳回去
    /// </summary>
    /// <param name="timeout">覆盖原有超时</param>
    /// <returns>
    /// <para><see cref="ApiStatusType"/> API执行状态</para>
    /// <para><see langword="messageId"/> 发送消息的id</para>
    /// </returns>
    public async ValueTask<(ApiStatus apiStatus, int messageId)> PokeBack(TimeSpan? timeout = null)
    {
        return await SoraApi.SendGroupMessage(SourceGroup, SoraSegment.Poke(SendUser), timeout);
    }

#endregion 快捷方法

#region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="pokeEventArgs">戳一戳事件参数</param>
    internal GroupPokeEventArgs(Guid                       serviceId,
                                Guid                       connectionId,
                                string                     eventName,
                                OnebotPokeOrLuckyEventArgs pokeEventArgs)
        : base(serviceId, connectionId, eventName, pokeEventArgs.SelfId, pokeEventArgs.Time, SourceFlag.Group)
    {
        SendUser    = new User(serviceId, connectionId, pokeEventArgs.UserId);
        TargetUser  = new User(serviceId, connectionId, pokeEventArgs.TargetId);
        SourceGroup = new Group(connectionId, pokeEventArgs.GroupId);
    }

#endregion 构造函数
}