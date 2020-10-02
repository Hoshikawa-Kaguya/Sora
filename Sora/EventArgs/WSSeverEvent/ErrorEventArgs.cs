using System;
using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    public sealed class ErrorEventArgs : ServerBaseEventArgs
    {
        #region 属性
        /// <summary>
        /// 错误
        /// </summary>
        public Exception Exception { get; set; }
        #endregion

        public ErrorEventArgs(Exception ex, IWebSocketConnectionInfo connectionInfo)
        {
            this.Exception      = ex;
            base.ConnectionInfo = connectionInfo;
        }
    }
}
