using System;
using Sora.EventArgs.OnebotEvent.MessageEvent;

namespace Sora.EventArgs.SoraEvent
{
    public sealed class PrivateMessageEventArgs : BaseSoraEventArgs
    {
        #region 构造函数

        internal PrivateMessageEventArgs(Guid connectionGuid, string eventName, ServerPrivateMsgEventArgs serverGroupMsg)
            : base(connectionGuid, eventName, serverGroupMsg.SelfID, serverGroupMsg.Time)
        {
            //serverGroupMsg.SenderInfo
        }

        #endregion 
    }
}
