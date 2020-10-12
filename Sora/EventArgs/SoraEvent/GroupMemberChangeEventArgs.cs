using System;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    public class GroupMemberChangeEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 退群成员
        /// </summary>
        public User LeaveUser { get; private set; }

        /// <summary>
        /// 执行者
        /// </summary>
        public User Operator { get; private set; }

        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 事件子类型
        /// </summary>
        internal MemberChangeType SubType { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="groupMemberChange">服务端上传文件通知参数</param>
        internal GroupMemberChangeEventArgs(Guid connectionGuid, string eventName, ServerGroupMemberChangeEventArgs groupMemberChange) :
            base(connectionGuid, eventName, groupMemberChange.SelfID, groupMemberChange.Time)
        {
            this.LeaveUser   = new User(connectionGuid, groupMemberChange.UserId);
            this.Operator    = new User(connectionGuid, groupMemberChange.OperatorId);
            this.SourceGroup = new Group(connectionGuid, groupMemberChange.GroupId);
            this.SubType     = groupMemberChange.SubType;
        }
        #endregion
    }
}
