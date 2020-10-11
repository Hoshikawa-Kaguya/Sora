using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.MessageEvent;
using Sora.Module.ApiMessageModel;
using Sora.Module.CQCodes;
using Sora.Module.SoraModel;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 群消息事件参数
    /// </summary>
    public sealed class GroupMessageEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 消息内容
        /// </summary>
        public Message Message { get; private set; }

        /// <summary>
        /// 是否来源于匿名群成员
        /// </summary>
        public bool IsAnonymousMessage { get; private set; }

        /// <summary>
        /// 消息发送者实例
        /// </summary>
        public User Sender { get; private set; }

        /// <summary>
        /// 发送者信息
        /// </summary>
        public GroupSenderInfo SenderInfo { get; private set; }

        /// <summary>
        /// 消息来源群组实例
        /// </summary>
        public Group SourceGroup { get; private set; }
        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="serverGroupMsg">服务器消息事件</param>
        internal GroupMessageEventArgs(Guid connectionGuid, string eventName, ServerGroupMsgEventArgs serverGroupMsg
        ) : base(connectionGuid, eventName, serverGroupMsg.SelfID, serverGroupMsg.Time)
        {
            this.IsAnonymousMessage = serverGroupMsg.Anonymous == null;
            this.Message = new Message(connectionGuid, serverGroupMsg.MessageId, serverGroupMsg.RawMessage,
                                       MessageParse.ParseMessageList(serverGroupMsg.MessageList), serverGroupMsg.Time,
                                       serverGroupMsg.Font);
            this.Sender             = new User(connectionGuid, serverGroupMsg.UserId);
            this.SourceGroup        = new Group(connectionGuid, serverGroupMsg.GroupId);
            this.SenderInfo         = serverGroupMsg.SenderInfo;
        }
        #endregion

        #region 快捷方法
        /// <summary>
        /// 快速回复
        /// </summary>
        /// <param name="message">消息内容</param>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> QuickReply(params object[] message)
        {
            return await base.SoraApi.SendGroupMessage(SourceGroup.Id, message);
        }

        /// <summary>
        /// 撤回发送者消息
        /// </summary>
        public async ValueTask DeleteSourceMessage()
        {
            await base.SoraApi.DeleteMessage(this.Message.MessageId);
        }
        #endregion
    }
}
