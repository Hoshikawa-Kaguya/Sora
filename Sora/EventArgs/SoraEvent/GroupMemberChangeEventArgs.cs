using System;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    public sealed class GroupMemberChangeEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 变更成员
        /// </summary>
        public User ChangedUser { get; private set; }

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
        /// <param name="groupMemberChange">API上传文件通知参数</param>
        internal GroupMemberChangeEventArgs(Guid connectionGuid, string eventName, ApiGroupMemberChangeEventArgs groupMemberChange) :
            base(connectionGuid, eventName, groupMemberChange.SelfID, groupMemberChange.Time)
        {
            this.ChangedUser   = new User(connectionGuid, groupMemberChange.UserId);
            //执行者和变动成员可能为同一人
            this.Operator = groupMemberChange.UserId == groupMemberChange.OperatorId
                ? this.ChangedUser
                : new User(connectionGuid, groupMemberChange.OperatorId);
            this.SourceGroup = new Group(connectionGuid, groupMemberChange.GroupId);
            this.SubType     = groupMemberChange.SubType;
        }
        #endregion
    }
}
