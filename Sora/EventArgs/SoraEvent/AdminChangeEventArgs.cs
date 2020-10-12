using System;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 管理员变动事件参数
    /// </summary>
    public class AdminChangeEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 上传者
        /// </summary>
        public User Sender { get; private set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public AdminChangeType SubType { get; private set; }
        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="adminChange">服务端管理员变动事件参数</param>
        internal AdminChangeEventArgs(Guid connectionGuid, string eventName, ServerAdminChangeEventArgs adminChange) :
            base(connectionGuid, eventName, adminChange.SelfID, adminChange.Time)
        {
            this.SourceGroup = new Group(connectionGuid, adminChange.GroupId);
            this.Sender      = new User(connectionGuid, adminChange.UserId);
            this.SubType     = adminChange.SubType;
        }
        #endregion
    }
}
