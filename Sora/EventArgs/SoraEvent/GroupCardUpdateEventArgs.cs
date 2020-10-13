using System;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 成员名片变更事件参数
    /// </summary>
    public sealed class GroupCardUpdateEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 名片改变的成员
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 新名片
        /// </summary>
        public string NewCard { get; private set; }

        /// <summary>
        /// 旧名片
        /// </summary>
        public string OldCard { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="groupCardUpdate">群名片更新事件参数</param>
        internal GroupCardUpdateEventArgs(Guid connectionGuid, string eventName, ApiGroupCardUpdateEventArgs groupCardUpdate) :
            base(connectionGuid, eventName, groupCardUpdate.SelfID, groupCardUpdate.Time)
        {
            this.User        = new User(connectionGuid,groupCardUpdate.UserId);
            this.SourceGroup = new Group(connectionGuid, groupCardUpdate.GroupId);
            this.NewCard     = groupCardUpdate.NewCard;
            this.OldCard     = groupCardUpdate.OldCard;
        }
        #endregion
    }
}
