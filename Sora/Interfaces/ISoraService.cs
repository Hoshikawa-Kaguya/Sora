using System.Threading.Tasks;
using Sora.Net;
using Sora.OnebotAdapter;

namespace Sora.Interfaces;

/// <summary>
/// Sora 连接服务
/// </summary>
public interface ISoraService
{
    /// <summary>
    /// 事件接口    
    /// </summary>
    EventAdapter Event { get; }

    /// <summary>
    /// 服务器连接管理器
    /// </summary>
    ConnectionManager ConnManager { get; }

    /// <summary>
    /// 启动 Sora 服务
    /// </summary>
    ValueTask StartService();
}