using System;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 好友消息撤回事件
    /// </summary>
    public sealed class FriendRecallEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 消息发送者
        /// </summary>
        public User Sender { get; private set; }

        /// <summary>
        /// 被撤回的消息ID
        /// </summary>
        public int MessageId { get; private set; }
        #endregion

        #region 构造函数

        internal FriendRecallEventArgs(Guid connectionGuid, string eventName, ApiFriendRecallEventArgs friendRecall) :
            base(connectionGuid, eventName, friendRecall.SelfID, friendRecall.Time)
        {
            this.Sender    = new User(connectionGuid, friendRecall.UserId);
            this.MessageId = friendRecall.MessageId;
        }
        #endregion
    }
}
