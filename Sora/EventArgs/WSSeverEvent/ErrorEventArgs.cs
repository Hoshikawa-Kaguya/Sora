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

        #region 构造函数
        public ErrorEventArgs(Exception ex, IWebSocketConnectionInfo connectionInfo)
        {
            this.Exception      = ex;
            base.ConnectionInfo = connectionInfo;
        }
        #endregion
    }
}
