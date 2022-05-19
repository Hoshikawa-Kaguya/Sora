using System;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Net;
using Sora.OnebotAdapter;

namespace Sora.Interfaces;

/// <summary>
/// Sora 连接服务
/// </summary>
public interface ISoraService : IDisposable
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
    /// 服务ID
    /// </summary>
    Guid ServiceId { get; }

    /// <summary>
    /// 获取API实例
    /// </summary>
    /// <param name="connectionId">链接ID</param>
    SoraApi GetApi(Guid connectionId);

    /// <summary>
    /// 启动 Sora 服务
    /// </summary>
    ValueTask StartService();

    /// <summary>
    /// 停止 Sora 服务
    /// </summary>
    ValueTask StopService();
}