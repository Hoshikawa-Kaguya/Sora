using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Sora.EventArgs.WSSeverEvent;
using Sora.Model;
using Sora.Tool;
using Sora.TypeEnum;

namespace Sora
{
    public class OnebotWSServer : IDisposable
    {
        #region 属性
        /// <summary>
        /// 服务器配置类
        /// </summary>
        private ServerConfig Config { get; set; }

        /// <summary>
        /// WS服务器
        /// </summary>
        private WebSocketServer Server { get; set; }

        /// <summary>
        /// 链接信息
        /// </summary>
        private readonly Dictionary<Guid, IWebSocketConnection> ConnectionInfos = new Dictionary<Guid, IWebSocketConnection>();

        /// <summary>
        /// 心跳包检查
        /// </summary>
        private static Dictionary<Guid,Timer> HeartBeatTimers = new Dictionary<Guid, Timer>();

        /// <summary>
        /// 事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="selfId">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask AsyncCallBackHandler<in TEventArgs>(string selfId, TEventArgs eventArgs)where TEventArgs : System.EventArgs;
        #endregion

        #region 回调事件
        /// <summary>
        /// 心跳包处理回调
        /// </summary>
        public event AsyncCallBackHandler<PongEventArgs> OnPongAsync;
        /// <summary>
        /// 打开连接回调
        /// </summary>
        public event AsyncCallBackHandler<ConnectionEventArgs> OnOpenConnectionAsync;
        /// <summary>
        /// 关闭连接回调
        /// </summary>
        public event AsyncCallBackHandler<ConnectionEventArgs> OnCloseConnectionAsync;
        /// <summary>
        /// 错误回调
        /// </summary>
        public event AsyncCallBackHandler<ErrorEventArgs> OnErrorAsync; 
        #endregion

        #region 构造函数

        /// <summary>
        /// 创建一个反向WS客户端
        /// </summary>
        /// <param name="config">服务器配置</param>
        public OnebotWSServer(ServerConfig config)
        {
            //检查参数
            if(config == null) throw new ArgumentNullException(nameof(config));
            if (config.Port < 0 || config.Port > 65535) throw new ArgumentOutOfRangeException(nameof(config.Port));

            this.Config = config;

            //禁用原log
            FleckLog.Level = LogLevel.Error;
            this.Server    = new WebSocketServer($"ws://0.0.0.0:{config.Port}");
        }
        #endregion

        #region 服务端启动
        //Todo DEBUG模式
        /// <summary>
        /// 启动WS服务端
        /// </summary>
        public void Start()
        {
            Server.Start(socket =>
                         {
                             //接收事件处理
                             //获取请求头数据
                             if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                                            out string selfId) ||
                                 !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                                           out string role)){return;}
                             //获取连接类型
                             Enum.TryParse(role, out ConnectionType type);
                             //请求路径检查
                             bool isLost;
                             switch (type)
                             {
                                 case ConnectionType.Universal:
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.UniversalPath);
                                     break;
                                 case ConnectionType.Event:
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.EventPath);
                                     break;
                                 case ConnectionType.Api:
                                     isLost = !socket.ConnectionInfo.Path.Trim('/').Equals(Config.ApiPath);
                                     break;
                                 default:
                                     isLost = false;
                                     break;
                             }
                             if (isLost)
                             {
                                 socket.Close();
                                 ConsoleLog.Warning("Sora",
                                                 $"Client lost({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                 return;
                             }
                             //心跳包
                             socket.OnPong = async (echo) =>
                                             {
                                                 if (OnPongAsync == null) { return; }
                                                 //心跳包事件处理
                                                 await Task.Run(() =>
                                                                {
                                                                    OnPongAsync(selfId, 
                                                                                    new PongEventArgs(echo,
                                                                                        socket.ConnectionInfo));
                                                                });
                                             };
                             //打开连接
                             socket.OnOpen = async () =>
                                             {
                                                 //获取Token
                                                 if (socket.ConnectionInfo.Headers.TryGetValue("Authorization",out string token))
                                                 {
                                                     //验证Token
                                                     if(!token.Equals(this.Config.AccessToken)) return;
                                                 }
                                                 //向客户端发送Ping
                                                 await socket.SendPing(new byte[] { 1, 2, 5 });
                                                 //事件回调
                                                 ConnectionEventArgs connection =
                                                     new ConnectionEventArgs(type, socket.ConnectionInfo);
                                                 if (OnOpenConnectionAsync != null)
                                                 {
                                                     await Task.Run(() =>
                                                                    {
                                                                        OnOpenConnectionAsync(selfId, connection);
                                                                    });
                                                 }
                                                 ConnectionInfos.Add(socket.ConnectionInfo.Id, socket);
                                                 ConsoleLog.Info("Sora",
                                                                 $"Client connected({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                             };
                             //关闭连接
                             socket.OnClose = async () =>
                                              {
                                                  //移除原连接信息
                                                  if (ConnectionInfos.Any(conn => conn.Key == socket.ConnectionInfo.Id))
                                                  {
                                                      ConnectionInfos.Remove(socket.ConnectionInfo.Id);
                                                      if (OnCloseConnectionAsync != null)
                                                      {
                                                          await Task.Run(() =>
                                                                         {
                                                                             OnCloseConnectionAsync(selfId,
                                                                                 new ConnectionEventArgs(type,
                                                                                     socket.ConnectionInfo));
                                                                         });
                                                      }
                                                  }
                                                  ConsoleLog.Info("Sora",
                                                                     $"Client closed connection({socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort})");
                                              };
                             //上报接收
                             socket.OnMessage = async (message) =>
                                                {
                                                    //处理接收的数据
                                                    if (ConnectionInfos.Any(conn => conn.Key == socket.ConnectionInfo.Id))
                                                    {
                                                        try
                                                        {
                                                            Console.WriteLine($"=================\nselfId = {selfId}\ntype = {type}\nmessage = {message.Trim()}\nclient path = {socket.ConnectionInfo.Path}");
                                                            //TODO 数据反序列化
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Console.WriteLine(e);
                                                            if (OnErrorAsync != null)
                                                            {
                                                                await Task.Run(() =>
                                                                               {
                                                                                   OnErrorAsync(selfId,
                                                                                       new ErrorEventArgs(e, socket.ConnectionInfo));
                                                                               });
                                                            }
                                                        }
                                                    }
                                                    ConsoleLog.Debug("Sora",
                                                                     $"Client message({message})");
                                                };
                         });
            ConsoleLog.Info("Sora",$"Server running at 0.0.0.0:{Config.Port}");
        }
        ~OnebotWSServer()
        {
            Dispose();
        }
        public void Dispose()
        {
            Server?.Dispose();
            ConnectionInfos.Clear();
        }
        #endregion

        #region 私有方法
        //TODO 事件分发
        #endregion
    }
}
