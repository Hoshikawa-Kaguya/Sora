using System;
using System.Threading.Tasks;
using Sora.Entities;
using Sora.Enumeration;
using Sora.OnebotModel.OnebotEvent.RequestEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 好友请求事件参数
/// </summary>
public sealed class FriendRequestEventArgs : BaseSoraEventArgs
{
#region 属性

    /// <summary>
    /// 请求发送者实例
    /// </summary>
    public User Sender { get; }

    /// <summary>
    /// 验证信息
    /// </summary>
    public string Comment { get; }

    /// <summary>
    /// 当前请求的flag标识
    /// </summary>
    public string RequestFlag { get; }

#endregion

#region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="friendObRequestArgs">好友申请事件参数</param>
    internal FriendRequestEventArgs(Guid                           serviceId,
                                    Guid                           connectionId,
                                    string                         eventName,
                                    OnebotFriendObRequestEventArgs friendObRequestArgs)
        : base(serviceId,
               connectionId,
               eventName,
               friendObRequestArgs.SelfId,
               friendObRequestArgs.Time,
               SourceFlag.Private)
    {
        Sender      = new User(serviceId, connectionId, friendObRequestArgs.UserId);
        Comment     = friendObRequestArgs.Comment;
        RequestFlag = friendObRequestArgs.Flag;
    }

#endregion

#region 公有方法

    /// <summary>
    /// 同意当前申请
    /// </summary>
    /// <param name="remark">设置备注</param>
    public async ValueTask Accept(string remark = null)
    {
        await SoraApi.SetFriendAddRequest(RequestFlag, true, remark);
    }

    /// <summary>
    /// 拒绝当前申请
    /// </summary>
    public async ValueTask Reject()
    {
        await SoraApi.SetFriendAddRequest(RequestFlag, false);
    }

#endregion
}