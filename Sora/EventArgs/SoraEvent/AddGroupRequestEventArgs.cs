using System;
using System.Threading.Tasks;
using Sora.Entities;
using Sora.Enumeration.EventParamsType;
using Sora.OnebotModel.OnebotEvent.RequestEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 入群申请
/// </summary>
public sealed class AddGroupRequestEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 请求发送者实例
    /// </summary>
    public User Sender { get; private set; }

    /// <summary>
    /// 请求发送到的群组实例
    /// </summary>
    public Group SourceGroup { get; private set; }

    /// <summary>
    /// 验证信息
    /// </summary>
    public string Comment { get; private set; }

    /// <summary>
    /// 当前请求的 flag 标识
    /// </summary>
    public string RequestFlag { get; private set; }

    /// <summary>
    /// 请求子类型
    /// </summary>
    public GroupRequestType SubType { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="groupRequestArgs">加群申请事件参数</param>
    internal AddGroupRequestEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                      OnebotGroupRequestEventArgs groupRequestArgs) :
        base(serviceId, connectionId, eventName, groupRequestArgs.SelfID, groupRequestArgs.Time)
    {
        Sender      = new User(serviceId, connectionId, groupRequestArgs.UserId);
        SourceGroup = new Group(serviceId, connectionId, groupRequestArgs.GroupId);
        Comment     = groupRequestArgs.Comment;
        RequestFlag = groupRequestArgs.Flag;
        SubType     = groupRequestArgs.GroupRequestType;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 同意当前申请
    /// </summary>
    public async ValueTask Accept()
    {
        await SoraApi.SetGroupAddRequest(RequestFlag, SubType, true);
    }

    /// <summary>
    /// 拒绝当前申请
    /// </summary>
    /// <param name="reason">原因</param>
    public async ValueTask Reject(string reason = null)
    {
        await SoraApi.SetGroupAddRequest(RequestFlag, SubType, false, reason);
    }

    #endregion
}