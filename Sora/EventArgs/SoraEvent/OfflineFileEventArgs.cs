using System;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent;

/// <summary>
/// 接收到离线文件事件参数
/// </summary>
public class OfflineFileEventArgs : BaseSoraEventArgs
{
    #region 属性

    /// <summary>
    /// 文件发送者
    /// </summary>
    public User Sender { get; private set; }

    /// <summary>
    /// 离线文件信息
    /// </summary>
    public OfflineFileInfo OfflineFileInfo { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器链接标识</param>
    /// <param name="eventName">事件名</param>
    /// <param name="offlineFileArgs">离线文件事件参数</param>
    internal OfflineFileEventArgs(Guid serviceId, Guid connectionId, string eventName,
                                  OnebotOfflineFileEventArgs offlineFileArgs) :
        base(serviceId, connectionId, eventName, offlineFileArgs.SelfID, offlineFileArgs.Time)
    {
        Sender          = new User(serviceId, connectionId, offlineFileArgs.UserId);
        OfflineFileInfo = offlineFileArgs.Info;
    }

    #endregion
}