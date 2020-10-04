using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    /// <summary>
    /// 服务器事件基类
    /// </summary>
    public class BaseServerEventArgs : System.EventArgs
    {
        /// <summary>
        /// 链接信息
        /// </summary>
        internal IWebSocketConnectionInfo ConnectionInfo { set; get; }
    }
}
