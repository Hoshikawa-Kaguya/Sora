using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    public class ServerBaseEventArgs : System.EventArgs
    {
        /// <summary>
        /// 链接信息
        /// </summary>
        internal IWebSocketConnectionInfo ConnectionInfo { set; get; }
    }
}
