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
using Sora.Net.Records;
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
    public Guid ServiceId { get; } = Guid.NewGuid();

#endregion

#region 私有字段

    /// <summary>
    /// 客户端已准备启动标识
    /// </summary>
    internal readonly bool _isReady;

    /// <summary>
    /// 客户端已启动
    /// </summary>
    internal bool _isRunning;

    /// <summary>
    /// dispose flag
    /// </summary>
    internal bool _disposed;

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
        _isReady = false;
        Log.Info("Sora", $"Sora WebSocket客户端初始化... [{ServiceId}]");
        Config = config ?? throw new ArgumentNullException(nameof(config));
        //写入初始化信息
        if (!ServiceRecord.AddOrUpdateRecord(ServiceId, new ServiceConfig(config), this))
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
        //全局异常事件
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.UnhandledExceptionLog(args);
            crashAction(args.ExceptionObject as Exception);
        };
        _isReady = true;
    }

#endregion

#region 客户端启停

    /// <summary>
    /// <para>启动 Sora 服务</para>
    /// <para>启动客户端并自动连接服务器</para>
    /// </summary>
    public async ValueTask StartService()
    {
        if (!_isReady)
        {
            Log.Warning("Sora", "服务已经启动了！");
            return;
        }

        //处理连接路径
        string serverPath = string.IsNullOrEmpty(Config.UniversalPath)
            ? $"ws://{Config.Host}:{Config.Port}"
            : $"ws://{Config.Host}:{Config.Port}/{Config.UniversalPath.Trim('/')}/";
        Log.Debug("Sora", $"Onebot server addr:{serverPath}");
        Client = new WebsocketClient(new Uri(serverPath), CreateSocket)
        {
            ReconnectTimeout      = Config.ReconnectTimeOut,
            ErrorReconnectTimeout = Config.ReconnectTimeOut
        };
        //消息接收事件
        _subClientMessageReceived = Client.MessageReceived.Subscribe(msg => Task.Run(async () =>
        {
            if (_disposed || string.IsNullOrEmpty(msg.Text))
                return;
            await Event.Adapter(JObject.Parse(msg.Text), ServiceId);
        }));
        //连接断开事件
        _subClientDisconnectionHappened = Client.DisconnectionHappened.Subscribe(info => Task.Run(() =>
        {
            if (_disposed)
                return;
            //移除原连接信息
            if (ConnectionRecord.Exists(ServiceId))
                ConnManager.CloseConnection(ServiceId);

            if (info.Exception != null)
                Log.Error("Sora", $"监听服务器时发生错误\r\n{Log.ErrorLogBuilder(info.Exception)}");
            else
                Log.Info("Sora", "服务器连接被关闭");
        }));
        //重连事件
        _subClientReconnectionHappened = Client.ReconnectionHappened.Subscribe(info => Task.Run(() =>
        {
            if (_disposed)
                return;
            if (info.Type == ReconnectionType.Initial || !_isRunning)
                return;
            Log.Info("Sora", $"服务器已自动重连{info.Type}");
            ConnManager.OpenConnection("Universal",
                                       "0",
                                       new ClientSocket(Client),
                                       ServiceId,
                                       ServiceId,
                                       Config.ApiTimeOut);
        }));
        //开始客户端
        await Client.Start();
        if (!Client.IsRunning || !Client.IsStarted)
            throw new WebSocketClientException("WebSocket client is not running");

        ConnManager.OpenConnection("Universal", "0", new ClientSocket(Client), ServiceId, ServiceId, Config.ApiTimeOut);
        Log.Info("Sora", "Sora WebSocket客户端正在运行并已连接至onebot服务器");
        _isRunning = true;
    }

    /// <summary>
    /// <para>停止 Sora 服务</para>
    /// <para>停止ws客户端</para>
    /// </summary>
    public async ValueTask StopService()
    {
        if (_disposed)
            return;
        Log.Warning("Sora", $"SoraWebsocket客户端[{ServiceId}]正在停止...");
        //取消Client上已注册的各事件订阅
        _subClientMessageReceived?.Dispose();
        _subClientDisconnectionHappened?.Dispose();
        _subClientReconnectionHappened?.Dispose();
        ConnManager.CloseAllConnection(ServiceId);
        //停止客户端
        await Client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
        Client.Dispose();
        _isRunning = false;
        Log.Warning("Sora", $"SoraWebSocket客户端[{ServiceId}]已停止");
    }

    /// <summary>
    /// GC析构函数
    /// </summary>
    ~SoraWebsocketClient()
    {
        Log.Warning("Destructor Call", $"Service[{ServiceId}]");
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
        Task.Delay(100).Wait();
        Helper.DisposeService(ServiceId);
        GC.SuppressFinalize(this);
    }

#endregion

#region util

    private ClientWebSocket CreateSocket()
    {
        ClientWebSocket clientWebSocket = new();
        if (!string.IsNullOrEmpty(Config.AccessToken))
            clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {Config.AccessToken}");
        return clientWebSocket;
    }

    /// <summary>
    /// 获取API实例
    /// </summary>
    /// <param name="connectionId">链接ID</param>
    public SoraApi GetApi(Guid connectionId)
    {
        return ConnectionRecord.GetApi(connectionId);
    }

#endregion
}