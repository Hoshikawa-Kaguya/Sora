using System;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 群禁言事件参数
    /// </summary>
    public class GroupMuteEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 被执行成员
        /// </summary>
        public User MuteUser { get; private set; }

        /// <summary>
        /// 执行者
        /// </summary>
        public User Operator { get; private set; }

        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 禁言时长(s)
        /// </summary>
        internal long Duration { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="groupMute">服务端群禁言事件参数</param>
        internal GroupMuteEventArgs(Guid connectionGuid, string eventName, ServerGroupMuteEventArgs groupMute) :
            base(connectionGuid, eventName, groupMute.SelfID, groupMute.Time)
        {
            this.MuteUser    = new User(connectionGuid, groupMute.UserId);
            this.Operator    = new User(connectionGuid, groupMute.OperatorId);
            this.SourceGroup = new Group(connectionGuid, groupMute.GroupId);
            this.Duration    = groupMute.Duration;
        }
        #endregion
    }
}
