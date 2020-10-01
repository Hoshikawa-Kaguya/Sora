using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    public sealed class ErrorEventArgs : BaseEventArgs
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
