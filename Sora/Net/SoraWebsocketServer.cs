using System;
using System.Data;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json.Linq;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Entities.Socket;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.OnebotInterface;
using Sora.Util;
using YukariToolBox.LightLog;
using LogLevel = Fleck.LogLevel;

namespace Sora.Net;

/// <summary>
/// Sora服务器实例
/// </summary>
public sealed class SoraWebsocketServer : ISoraService, IDisposable
{
    #region 属性

    /// <summary>
    /// 服务器配置类
    /// </summary>
    private ServerConfig Config { get; }

    /// <summary>
    /// WS服务器
    /// </summary>
    private WebSocketServer Server { get; }

    /// <summary>
    /// 事件接口
    /// </summary>
    public EventInterface Event { get; }

    /// <summary>
    /// 服务器连接管理器
    /// </summary>
    public ConnectionManager ConnManager { private set; get; }

    #endregion

    #region 私有字段

    /// <summary>
    /// 服务器已准备启动标识
    /// </summary>
    private readonly bool _serverReady;

    /// <summary>
    /// 服务器ID
    /// </summary>
    private readonly Guid _serverId = Guid.NewGuid();

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
        //检查端口占用
        if (Helper.IsPortInUse(config.Port))
        {
            var e = new InvalidOperationException($"端口{config.Port}已被占用，请更换其他端口");
            Log.Fatal(e, "Sora", $"端口{config.Port}已被占用，请更换其他端口", config);
            throw e;
        }

        //写入初始化信息
        if (!StaticVariable.ServiceInfos.TryAdd(_serverId, new ServiceInfo(_serverId, config)))
            throw new DataException("try add service info failed");
        _serverReady = false;
        Log.Info("Sora", $"Sora WebSocket服务器初始化... [{_serverId}]");
        //检查参数
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.Port == 0)
            throw new ArgumentOutOfRangeException(nameof(config.Port), "Port 0 is not allowed");
        //初始化连接管理器
        ConnManager = new ConnectionManager(config);
        Config      = config;
        //实例化事件接口
        Event = new EventInterface(_serverId, config.AutoMarkMessageRead);
        //禁用原log
        FleckLog.Level = (LogLevel) 4;
        Server = new WebSocketServer($"ws://{config.Host}:{config.Port}")
        {
            //出错后进行重启
            RestartAfterListenError = true
        };
        //全局异常事件
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                                                      {
                                                          if (crashAction == null)
                                                              Helper.FriendlyException(args);
                                                          else
                                                              crashAction(args.ExceptionObject as Exception);
                                                      };
        _serverReady = true;
    }

    #endregion

    #region 服务端启动

    /// <summary>
    /// 启动 Sora 服务
    /// </summary>
    public ValueTask StartService()
    {
        if (!_serverReady) return ValueTask.CompletedTask;
        //启动服务器
        Server.Start(SocketConfig);
        ConnManager.StartTimer(_serverId);
        Log.Info("Sora", $"Sora WebSocket服务器正在运行[{Config.Host}:{Config.Port}]");
        return ValueTask.CompletedTask;
    }

    private void SocketConfig(IWebSocketConnection socket)
    {
        //接收事件处理
        //获取请求头数据
        if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out var selfId) || //bot UID
            !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out var role)) //Client Type
            return;

        //请求路径检查
        var isLost = role switch
        {
            "Universal" => !socket.ConnectionInfo.Path.Trim('/').Equals(Config.UniversalPath.Trim('/')),
            _ => true
        };
        if (isLost)
        {
            socket.Close();
            Log.Warning("Sora",
                        $"关闭与未知客户端的连接[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]，请检查是否设置正确的监听地址");
            return;
        }

        //打开连接
        socket.OnOpen = () =>
                        {
                            //获取Token
                            if (socket.ConnectionInfo.Headers.TryGetValue("Authorization",
                                                                          out var headerValue))
                            {
                                var token = headerValue.Split(' ')[1];
                                Log.Debug("Server", $"get token = {token}");
                                //验证Token
                                if (!token.Equals(Config.AccessToken)) return;
                            }

                            //向客户端发送Ping
                            socket.SendPing(new byte[] {1, 2, 5});
                            //事件回调
                            ConnManager.OpenConnection(role, selfId, new ServerSocket(socket), _serverId,
                                                       socket.ConnectionInfo.Id, Config.ApiTimeOut);
                            Log.Info("Sora",
                                     $"已连接客户端[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                        };
        //关闭连接
        socket.OnClose = () =>
                         {
                             //移除原连接信息
                             if (ConnManager.ConnectionExists(socket.ConnectionInfo.Id))
                                 ConnManager.CloseConnection(role, Convert.ToInt64(selfId),
                                                             socket.ConnectionInfo.Id);

                             Log.Info("Sora",
                                      $"客户端连接被关闭[{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                         };
        //上报接收
        socket.OnMessage = message =>
                               Task.Run(() => Event.Adapter(JObject.Parse(message), socket.ConnectionInfo.Id));
    }

    /// <summary>
    /// GC析构函数
    /// </summary>
    ~SoraWebsocketServer()
    {
        Dispose();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StaticVariable.CleanServiceInfo(_serverId);
        Server.Dispose();
        ConnManager.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}