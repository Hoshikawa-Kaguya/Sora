using System;
using System.Data;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Entities.Base;
using Sora.Entities.Socket;
using Sora.Exceptions;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.OnebotAdapter;
using Sora.Util;
using Websocket.Client;
using YukariToolBox.LightLog;

namespace Sora.Net;

/// <summary>
/// Sora正向WS链接客户端
/// </summary>
public sealed class SoraWebsocketClient : ISoraService
{
    #region 属性

    /// <summary>
    /// 服务器配置类
    /// </summary>
    private ClientConfig Config { get; }

    /// <summary>
    /// WS客户端
    /// </summary>
    private WebsocketClient Client { get; set; }

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
    public Guid ServiceId => _clientId;

    #endregion

    #region 私有字段

    /// <summary>
    /// 客户端已准备启动标识
    /// </summary>
    private readonly bool _clientReady;

    /// <summary>
    /// 客户端ID
    /// </summary>
    private readonly Guid _clientId = Guid.NewGuid();

    /// <summary>
    /// 客户端已启动
    /// </summary>
    private bool _clientIsRunning;

    /// <summary>
    /// dispose flag
    /// </summary>
    private bool _disposed;

    //ws客户端事件订阅
    private IDisposable _subClientMessageReceived;
    private IDisposable _subClientDisconnectionHappened;
    private IDisposable _subClientReconnectionHappened;

    #endregion

    #region 构造方法

    /// <summary>
    /// 创建一个正向WS客户端
    /// </summary>
    /// <param name="config">配置文件</param>
    /// <param name="crashAction">发生未处理异常时的回调</param>
    /// <exception cref="DataException">数据初始化错误</exception>
    /// <exception cref="ArgumentNullException">空配置文件错误</exception>
    /// <exception cref="ArgumentOutOfRangeException">参数错误</exception>
    internal SoraWebsocketClient(ClientConfig config, Action<Exception> crashAction = null)
    {
        _clientReady = false;
        Log.Info("Sora", $"Sora WebSocket客户端初始化... [{_clientId}]");
        Config = config ?? throw new ArgumentNullException(nameof(config));
        //写入初始化信息
        if (!StaticVariable.ServiceConfigs.TryAdd(_clientId, new ServiceConfig(config)))
            throw new DataException("try add service config failed");
        //检查参数
        if (Config.Port == 0)
            throw new ArgumentOutOfRangeException(nameof(Config.Port), "Port 0 is not allowed");
        //初始化连接管理器
        ConnManager = new ConnectionManager(Config, _clientId);
        //实例化事件接口
        Event = new EventAdapter(_clientId, Config.ThrowCommandException, Config.SendCommandErrMsg);
        //全局异常事件
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (crashAction == null)
                Helper.FriendlyException(args);
            else
                crashAction(args.ExceptionObject as Exception);
        };
        _clientReady = true;
    }

    #endregion

    #region 客户端启停

    /// <summary>
    /// <para>启动 Sora 服务</para>
    /// <para>启动客户端并自动连接服务器</para>
    /// </summary>
    public async ValueTask StartService()
    {
        if (!_clientReady) return;
        //处理连接路径
        string serverPath = string.IsNullOrEmpty(Config.UniversalPath)
            ? $"ws://{Config.Host}:{Config.Port}"
            : $"ws://{Config.Host}:{Config.Port}/{Config.UniversalPath.Trim('/')}/";
        Log.Debug("Sora", $"Onebot服务器地址:{serverPath}");
        Client =
            new WebsocketClient(new Uri(serverPath), CreateSocket)
            {
                ReconnectTimeout      = Config.ReconnectTimeOut,
                ErrorReconnectTimeout = Config.ReconnectTimeOut
            };
        //消息接收事件
        _subClientMessageReceived = Client.MessageReceived.Subscribe(msg => Task.Run(() =>
        {
            if (_disposed) return;
            Event.Adapter(JObject.Parse(msg.Text), _clientId);
        }));
        //连接断开事件
        _subClientDisconnectionHappened =
            Client.DisconnectionHappened
                  .Subscribe(info => Task.Run(() =>
                   {
                       if (_disposed) return;
                       ConnectionManager.GetLoginUid(_clientId, out long uid);
                       //移除原连接信息
                       if (ConnectionManager.ConnectionExists(_clientId))
                           ConnManager.CloseConnection("Universal", uid, _clientId);

                       if (info.Exception != null)
                           Log.Error("Sora",
                               $"监听服务器时发生错误\r\n{Log.ErrorLogBuilder(info.Exception)}");
                       else
                           Log.Info("Sora", "服务器连接被关闭");
                   }));
        //重连事件
        _subClientReconnectionHappened =
            Client.ReconnectionHappened
                  .Subscribe(info => Task.Run(() =>
                   {
                       if (_disposed) return;
                       if (info.Type == ReconnectionType.Initial || !_clientIsRunning)
                           return;
                       Log.Info("Sora", $"服务器已自动重连{info.Type}");
                       ConnManager.OpenConnection("Universal", "0",
                           new ClientSocket(Client),
                           _clientId, _clientId,
                           Config.ApiTimeOut);
                   }));
        //开始客户端
        await Client.Start();
        if (!Client.IsRunning || !Client.IsStarted)
            throw new WebSocketClientException("WebSocket client is not running");

        ConnManager.OpenConnection("Universal", "0", new ClientSocket(Client), _clientId, _clientId,
            Config.ApiTimeOut);
        Log.Info("Sora", "Sora WebSocket客户端正在运行并已连接至onebot服务器");
        _clientIsRunning = true;
    }

    /// <summary>
    /// <para>停止 Sora 服务</para>
    /// <para>停止ws客户端</para>
    /// </summary>
    public async ValueTask StopService()
    {
        if (_disposed) return;
        Log.Warning("Sora", $"SoraWebsocket客户端[{ServiceId}]正在停止...");
        //取消Client上已注册的各事件订阅
        _subClientMessageReceived?.Dispose();
        _subClientDisconnectionHappened?.Dispose();
        _subClientReconnectionHappened?.Dispose();
        ConnManager.CloseAllConnection(ServiceId);
        //停止客户端
        await Client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
        Client.Dispose();
        _clientIsRunning = false;
        Log.Warning("Sora", $"SoraWebSocket客户端[{ServiceId}]已停止");
    }

    /// <summary>
    /// GC析构函数
    /// </summary>
    ~SoraWebsocketClient()
    {
        Dispose();
    }

    /// <summary>
    /// 释放资源并断开链接
    /// </summary>
    public void Dispose()
    {
        StopService().AsTask().Wait();
        Client?.Dispose();
        ConnManager?.Dispose();
        _disposed = true;
        StaticVariable.DisposeService(_clientId);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region util

    private ClientWebSocket CreateSocket()
    {
        var clientWebSocket = new ClientWebSocket();
        clientWebSocket.Options.SetRequestHeader("Authorization",
            $"Bearer {Config.AccessToken}");
        return clientWebSocket;
    }

    /// <summary>
    /// 获取API实例
    /// </summary>
    /// <param name="connectionId">链接ID</param>
    public SoraApi GetApi(Guid connectionId)
    {
        return StaticVariable.ConnectionInfos[connectionId].ApiInstance;
    }

    #endregion
}