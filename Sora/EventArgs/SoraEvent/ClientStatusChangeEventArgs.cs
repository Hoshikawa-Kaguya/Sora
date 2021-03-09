using System;
using Sora.Entities.Info;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 其他客户端在线状态变更事件参数
    /// </summary>
    public class ClientStatusChangeEventArgs : BaseSoraEventArgs
    {
        #region 属性

        /// <summary>
        /// 客户端信息
        /// </summary>
        public ClientInfo Client { get; private set; }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool Online { get; private set; }

        #endregion

        #region 构造方法

        internal ClientStatusChangeEventArgs(Guid connectionGuid, string eventName,
                                             ApiClientStatusEventArgs clientStatus) : base(connectionGuid, eventName,
            clientStatus.SelfID, clientStatus.Time)
        {
            this.Client = clientStatus.ClientInfo;
            this.Online = clientStatus.Online;
        }

        #endregion
    }
}