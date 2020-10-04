using System;
using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    /// <summary>
    /// 服务器错误事件
    /// </summary>
    public sealed class ErrorEventArgs : BaseServerEventArgs
    {
        #region 属性
        /// <summary>
        /// 错误
        /// </summary>
        public Exception Exception { get; set; }
        #endregion

        #region 构造函数
        internal ErrorEventArgs(Exception ex, IWebSocketConnectionInfo connectionInfo)
        {
            this.Exception      = ex;
            base.ConnectionInfo = connectionInfo;
        }
        #endregion
    }
}
