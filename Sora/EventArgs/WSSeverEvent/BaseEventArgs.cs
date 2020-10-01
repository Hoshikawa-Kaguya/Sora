using Fleck;

namespace Sora.EventArgs.WSSeverEvent
{
    public class BaseEventArgs : System.EventArgs
    {
        /// <summary>
        /// 链接信息
        /// </summary>
        public IWebSocketConnectionInfo ConnectionInfo { set; get; }
    }
}
