using System;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    public sealed class LuckyKingEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 红包发送者
        /// </summary>
        public User SendUser { get; private set; }

        /// <summary>
        /// 运气王
        /// </summary>
        public User TargetUser { get; private set; }

        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="luckyEvent">运气王事件参数</param>
        internal LuckyKingEventArgs(Guid connectionGuid, string eventName, ApiPokeOrLuckyEventArgs luckyEvent) :
            base(connectionGuid, eventName, luckyEvent.SelfID, luckyEvent.Time)
        {
            this.SendUser    = new User(connectionGuid, luckyEvent.UserId);
            this.TargetUser  = new User(connectionGuid, luckyEvent.TargetId);
            this.SourceGroup = new Group(connectionGuid, luckyEvent.GroupId);
        }
        #endregion
    }
}
