using System.Threading.Tasks;
using Sora.Exceptions;
using Sora.Net;
using Sora.OnebotInterface;

namespace Sora.Interfaces
{
    /// <summary>
    /// Sora 连接服务
    /// </summary>
    public interface ISoraService
    {
        /// <summary>
        /// 事件接口
        /// </summary>
        public EventInterface Event { get; }

        /// <summary>
        /// 服务器连接管理器
        /// </summary>
        public ConnectionManager ConnManager { get; }

        /// <summary>
        /// 启动 Sora 服务
        /// </summary>
        /// <exception cref="SoraServerIsRuningException">已有服务器在运行</exception>
        public ValueTask StartService();
    }
}