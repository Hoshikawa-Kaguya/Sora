using System;
using System.Data;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json.Linq;
using Sora.Entities.Base;
using Sora.Entities.Socket;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Net.Records;
using Sora.OnebotAdapter;
using Sora.Util;
using YukariToolBox.LightLog;
using LogLevel = Fleck.LogLevel;

namespace Sora.Net;

/// <summary>
/// Sora服务器实例
/// </summary>
public sealed class SoraWebsocketServer : ISoraService
{
#region 属性

    /// <summary>
    /// 服务器配置类
    /// </summary>
    private ServerConfig Config { get; }

    /// <summary>
    /// WS服务器
    /// </summary>
    private WebSocketServer Server { get; set; }

    /// <summary>
    /// 事件接口
    /// </summary>
    public EventAdapter Event { get; }

    /// <summary>
    /// 服务器连接管理器
    /// </summary>
    public ConnectionManager ConnManager { get; }

    /// <summary>
    /// 服务ID
    /// </summary>
    public Guid ServiceId { get; } = Guid.NewGuid();

#endregion

#region 私有字段

    /// <summary>
    /// 服务器已准备启动标识
    /// </summary>
    internal readonly bool _isReady;

    /// <summary>
    /// dispose flag
    /// </summary>
    internal bool _disposed;

    /// <summary>
    /// 运行标志位
    /// </summary>
    internal bool _isRunning;

#endregion

#region 构造函数

    /// <summary>
    /// 创建一个反向WS服务端
    /// </summary>
    /// <param name="config">服务器配置</param>
    /// <param name="crashAction">发生未处理异常时的回调</param>
    /// <exception cref="ArgumentNullException">读取到了空配置文件</exception>
    /// <exception cref="ArgumentOutOfRangeException">服务器启动参数错误</exception>
    internal SoraWebsocketServer(ServerConfig config, Action<Exception> crashAction = null)
    {
        _isReady = false;
        Log.Info("Sora", $"Sora WebSocket服务器初始化... [{ServiceId}]");
        Config = config ?? throw new ArgumentNullException(nameof(config));

        //写入初始化信息
        if (!ServiceRecord.AddOrUpdateRecord(ServiceId, new ServiceConfig(Config), this))
            throw new DataException("try add service config failed");
        //检查参数
        if (Config.Port == 0)
            throw new ArgumentOutOfRangeException(nameof(Config.Port), "Port 0 is not allowed");
        //初始化连接管理器
        ConnManager = new ConnectionManager(Config, ServiceId);
        //实例化事件接口
        Event = new EventAdapter(ServiceId,
                                 Config.ThrowCommandException,
                                 Config.SendCommandErrMsg,
                                 Config.CommandExceptionHandle);
        //禁用原log
        FleckLog.Level = (LogLevel)4;
        //全局异常事件
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.UnhandledExceptionLog(args);
            crashAction(args.ExceptionObject as Exception);
        };
        _isReady = true;
    }

#endregion

#region 服务端启动

    /// <summary>
    /// 启动 Sora 服务
    /// </summary>
    public ValueTask StartService()
    {
        if (!_isReady)
        {
            Log.Warning("Sora", "服务已经启动了！");
            return ValueTask.CompletedTask;
        }

        //启动服务器
        Server = new WebSocketServer($"ws://{Config.Host}:{Config.Port}")
        {
            //出错后进行重启
            RestartAfterListenError = true
        };
        Server.Start(SocketEvent);
        Log.Info("Sora", $"Sora WebSocket服务器正在运行[{Config.Host}:{Config.Port}]");
        _isRunning = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// <para>停止 Sora 服务</para>
    /// <para>停止ws服务器</para>
    /// </summary>
    public ValueTask StopService()
    {
        if (_disposed && !_isRunning)
            return ValueTask.CompletedTask;
        Log.Warning("Sora", $"SoraWebsocket服务器[{ServiceId}]正在停止...");
        ConnManager.CloseAllConnection(ServiceId);
        //停止服务器
        Server.Dispose();
        _isRunning = false;
        Log.Warning("Sora", $"Sora WebSocket服务器[{ServiceId}]已停止运行");
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// socket事件处理
    /// </summary>
    private void SocketEvent(IWebSocketConnection socket)
    {
        //打开连接
        socket.OnOpen = () =>
        {
            if (_disposed || !_isRunning)
                return;
            // 接收事件处理
            if (!CheckRequest(socket, out string selfId))
                return;
            //事件回调
            ConnManager.OpenConnection("Universal",
                                       selfId,
                                       new ServerSocket(socket),
                                       ServiceId,
                                       socket.ConnectionInfo.Id,
                                       Config.ApiTimeOut);
            Log.Info("Sora", $"已连接客户端[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
        };
        //关闭连接
        socket.OnClose = () =>
        {
            if (_disposed || !_isRunning)
                return;
            //移除原连接信息
            if (ConnectionRecord.Exists(socket.ConnectionInfo.Id))
                ConnManager.CloseConnection(socket.ConnectionInfo.Id);
            Log.Info("Sora", $"客户端连接被关闭[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
        };
        //上报接收
        socket.OnMessage = message => Task.Run(async () =>
        {
            if (_disposed || !_isRunning)
                return;
            try
            {
                await Event.Adapter(JObject.Parse(message), socket.ConnectionInfo.Id);
            }
            catch (Exception e)
            {
                Helper.FriendlyException(e);
            }
        });
    }

    /// <summary>
    /// GC析构函数
    /// </summary>
    ~SoraWebsocketServer()
    {
        Log.Warning("Destructor Call", $"Service[{ServiceId}]");
        Dispose();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        //清理连接和ConnectionInfos
        StopService().AsTask().Wait();
        ConnManager?.Dispose();
        Task.Delay(100).Wait();
        //清除所有连接
        Helper.DisposeService(ServiceId);
        GC.SuppressFinalize(this);
    }

#endregion

#region util

    /// <summary>
    /// 获取API实例
    /// </summary>
    /// <param name="connectionId">链接ID</param>
    public SoraApi GetApi(Guid connectionId)
    {
        return ConnectionRecord.GetApi(connectionId);
    }

    private bool CheckRequest(IWebSocketConnection socket, out string selfId)
    {
        //获取请求头数据
        if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out selfId)
            ||                                                                            //bot UID
            !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out string role)) //Client Type
            return false;

        //请求路径检查
        bool isLost = role switch
                      {
                          "Universal" => !socket.ConnectionInfo.Path.Trim('/').Equals(Config.UniversalPath.Trim('/')),
                          _           => true
                      };
        if (isLost)
        {
            socket.Close();
            Log.Warning("Sora",
                        $"关闭与未知客户端的连接[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]，请检查是否设置正确的监听地址");
            return false;
        }

        //获取Token
        if (socket.ConnectionInfo.Headers.TryGetValue("Authorization", out string headerValue) && !string.IsNullOrEmpty(Config.AccessToken))
        {
            if(headerValue.Length <= 7)
                return false;
            string token = headerValue.Split(' ')[1];
            Log.Debug("Server", $"get token = {token}");
            //验证Token
            if (!token.Equals(Config.AccessToken))
                return false;
        }

        return true;
    }

#endregion
}