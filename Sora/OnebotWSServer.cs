using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fleck;
using Sora.EventArgs.WSSeverEvent;

namespace Sora
{
    public class OnebotWSServer : IDisposable
    {
        #region 属性
        /// <summary>
        /// 反向服务器端口
        /// </summary>
        private int Port { get; set; }

        /// <summary>
        /// 鉴权Token
        /// </summary>
        private string AccessToken { get; set; }

        /// <summary>
        /// API请求路径
        /// </summary>
        private string ApiPath { get; set; }

        /// <summary>
        /// Event请求路径
        /// </summary>
        private string EventPath { get; set; }

        /// <summary>
        /// WS服务器
        /// </summary>
        private WebSocketServer Server { get; set; }

        /// <summary>
        /// 链接信息
        /// </summary>
        private Dictionary<Guid, ConnectionEventArgs> ConnectionInfos { get; set; }

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
        /// 创建一个Universal反向WS客户端
        /// </summary>
        /// <param name="port"></param>
        /// <param name="accessToken"></param>
        /// <param name="universalPath"></param>
        public OnebotWSServer(int port, string accessToken = "", string universalPath = "ws")
        {
            //检查参数
            if(string.IsNullOrEmpty(universalPath)) throw new ArgumentNullException(nameof(universalPath));
            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            this.Port            = port;
            this.AccessToken     = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            this.ApiPath         = universalPath.Trim('/');
            this.EventPath       = universalPath.Trim('/');
            this.ConnectionInfos = new Dictionary<Guid, ConnectionEventArgs>();

            this.Server = new WebSocketServer($"ws://0.0.0.0:{Port}");
        }

        /// <summary>
        /// 创建一个反向WS客户端
        /// </summary>
        /// <param name="port"></param>
        /// <param name="accessToken"></param>
        /// <param name="apiPath"></param>
        /// <param name="eventPath"></param>
        public OnebotWSServer(int port, string accessToken = "", string apiPath = "api", string eventPath = "event")
        {
            //检查参数
            if(string.IsNullOrEmpty(apiPath) || string.IsNullOrEmpty(eventPath)) throw new NullReferenceException("apiPath or eventPath is null");
            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            this.Port            = port;
            this.AccessToken     = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            this.ApiPath         = apiPath.Trim('/');
            this.EventPath       = eventPath.Trim('/');
            this.ConnectionInfos = new Dictionary<Guid, ConnectionEventArgs>();

            this.Server = new WebSocketServer($"ws://0.0.0.0:{Port}");
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
                             //心跳包
                             socket.OnPong = async (echo) =>
                                             {
                                                 if (OnPongAsync == null) { return; }
                                                 if (socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out string selfId) == false) { return; }
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
                                                 //获取请求头数据
                                                 if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                         out string selfId) ||
                                                     !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                         out string type)){return;}
                                                 //获取Token
                                                 if (socket.ConnectionInfo.Headers.TryGetValue("Authorization",out string token))
                                                 {
                                                     //验证Token
                                                     if(!token.Equals(this.AccessToken)) return;
                                                 }
                                                 //向客户端发送Ping
                                                 await socket.SendPing(new byte[] { 1, 2, 5 });
                                                 //事件回调
                                                 ConnectionEventArgs connection = new ConnectionEventArgs(type,socket.ConnectionInfo);
                                                 if (OnOpenConnectionAsync != null)
                                                 {
                                                     await Task.Run(() =>
                                                                    {
                                                                        OnOpenConnectionAsync(selfId, connection);
                                                                    });
                                                 }
                                                 ConnectionInfos.Add(socket.ConnectionInfo.Id, connection);
                                             };
                             //关闭连接
                             socket.OnClose = async () =>
                                              {
                                                  //获取请求头数据
                                                  if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                          out string selfId) ||
                                                      !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                          out string type)){return;}
                                                  //移除原连接信息
                                                  if (ConnectionInfos.Any(conn => conn.Key == socket.ConnectionInfo.Id))
                                                  {
                                                      ConnectionInfos.Remove(socket.ConnectionInfo.Id,
                                                                             out ConnectionEventArgs eventArgs);
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
                                              };
                             //上报接收
                             socket.OnMessage = async (message) =>
                                                {
                                                    //获取请求头数据
                                                    if (!socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID",
                                                            out string selfId) ||
                                                        !socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role",
                                                            out string type)){return;}
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
                                                };
                         });
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
