using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Enumeration;
using Sora.Enumeration.ApiEnum;
using Sora.Model.CQCodes;
using Sora.Model.CQCodes.CQCodeModel;
using Sora.Model.Message;
using Sora.Model.OnebotApi;
using Sora.Model.SoraModel;
using Sora.Tool;

namespace Sora.OnebotInterface
{
    internal static class ApiInterface
    {
        #region 静态属性
        /// <summary>
        /// API超时时间
        /// </summary>
        internal static int TimeOut { get; set; }
        #endregion

        #region 请求表
        /// <summary>
        /// API请求等待列表
        /// </summary>
        internal static readonly List<Guid> RequestList = new List<Guid>();

        /// <summary>
        /// API响应被观察者
        /// </summary>
        private static readonly ISubject<Tuple<Guid, JObject>, Tuple<Guid, JObject>> OnebotSubject =
            new Subject<Tuple<Guid, JObject>>();
        #endregion

        #region API请求
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="target">发送目标uid</param>
        /// <param name="messages">发送的信息</param>
        /// <returns>
        /// message id
        /// </returns>
        internal static async ValueTask<(int retCode,int messageId)> SendPrivateMessage(Guid connection, long target, List<CQCode> messages)
        {
            ConsoleLog.Debug("Sora", "Sending send_msg request");
            if(messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //转换消息段列表
            List<OnebotMessage> messagesList = messages.Select(msg => msg.ToOnebotMessage()).ToList();
            //发送信息
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Private,
                    UserId      = target,
                    Message     = messagesList
                }
            }, connection);
            //处理API返回信息
            int code = GetBaseRetCode(ret).retCode;
            if (code != 0) return (code, -1);
            int msgCode = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode)
                ? messageCode
                : -1;
            ConsoleLog.Debug("Sora", $"Get send_msg response retcode={code}|msg_id={msgCode}");
            return (code, msgCode);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="target">发送目标gid</param>
        /// <param name="messages">发送的信息</param>
        /// <returns>
        /// ApiResponseCollection
        /// </returns>
        internal static async ValueTask<(int retCode,int messageId)> SendGroupMessage(Guid connection, long target, List<CQCode> messages)
        {
            ConsoleLog.Debug("Sora", "Sending send_msg request");
            if(messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //转换消息段列表
            List<OnebotMessage> messagesList = messages.Select(msg => msg.ToOnebotMessage()).ToList();
            //发送信息
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Group,
                    GroupId     = target,
                    Message     = messagesList
                }
            }, connection);
            //处理API返回信息
            int code = GetBaseRetCode(ret).retCode;
            if (code != 0) return (code, -1);
            int msgCode = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode)
                ? messageCode
                : -1;
            ConsoleLog.Debug("Sora", $"Get send_msg response retcode={code}|msg_id={msgCode}");
            return (code, msgCode);
        }

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">合并转发 ID</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<NodeArray> GetForwardMessage(Guid connection, string msgId)
        {
            if(string.IsNullOrEmpty(msgId)) throw new NullReferenceException(nameof(msgId));
            ConsoleLog.Debug("Sora", "Sending get_forward_msg request");
            //发送信息
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetForwardMessage,
                ApiParams = new GetForwardParams
                {
                    MessageId = msgId
                }
            }, connection);
            //处理API返回信息
            ConsoleLog.Debug("Sora", ret?["data"]);
            if (GetBaseRetCode(ret).retCode != 0) return new NodeArray();
            //转换消息类型
            NodeArray messageList =
                ret?["data"]?.ToObject<NodeArray>() ?? new NodeArray();
            messageList.ParseNode();
            return messageList;
        }

        /// <summary>
        /// 获取登陆账号信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<(int retCode, long uid, string nick)> GetLoginInfo(Guid connection)
        {
            ConsoleLog.Debug("Sora", "Sending get_login_info request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetLoginInfo
            }, connection);
            //处理API返回信息
            int code = GetBaseRetCode(ret).retCode;
            ConsoleLog.Debug("Sora", $"Get get_login_info response retcode={code}");
            if (code != 0) return (code, -1, null);
            return
            (
                retCode:code,
                uid:int.TryParse(ret["data"]?["user_id"]?.ToString(), out int uid) ? uid : -1,
                nick:ret["data"]?["nickname"]?.ToString() ?? string.Empty
            );
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(int retCode, ClientType clientType, string clientVer)> GetOnebotVersion(Guid connection)
        {
            ConsoleLog.Debug("Sora", "Sending get_version_info request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetVersion
            }, connection);
            //处理API返回信息
            int code = GetBaseRetCode(ret).retCode;
            ConsoleLog.Debug("Sora", $"Get get_version_info response retcode={code}");
            if (code != 0 || ret["data"] == null) return (code, ClientType.Other, "unknown");
            //判断是否为MiraiGo
            JObject.FromObject(ret["data"]).TryGetValue("go-cqhttp", out JToken clientJson);
            bool.TryParse(clientJson?.ToString()                ?? "false", out bool isGo);
            string verStr = ret["data"]?["version"]?.ToString() ?? string.Empty;
            //Go客户端
            if (isGo) return (code, ClientType.GoCqhttp,verStr);
            //其他客户端
            return (code, ClientType.Other,verStr);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>好友信息列表</returns>
        internal static async ValueTask<List<FriendInfo>> GetFriendList(Guid connection)
        {
            ConsoleLog.Debug("Sora","Sending get_friend_list request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetFriendList
            }, connection);
            //处理API返回信息
            int retCode = GetBaseRetCode(ret).retCode;
            ConsoleLog.Debug("Sora", $"Get get_friend_list response retcode={retCode}");
            if (retCode != 0 || ret["data"] == null) return new List<FriendInfo>();
            List<FriendInfo> friendList = new List<FriendInfo>();
            //处理返回的好友信息
            foreach (JToken token in ret["data"]?.ToArray())
            {
                friendList.Add(new FriendInfo
                {
                    User = new UserInfo
                    {
                        Id   = Convert.ToInt64(token["user_id"] ?? -1),
                        Nick = token["nickname"]?.ToString() ?? string.Empty
                    },
                    Remark = token["remark"]?.ToString() ?? string.Empty
                });
            }
            return friendList;
        }

        internal static async ValueTask<List<Group>> GetGroupList(Guid connection)
        {
            ConsoleLog.Debug("Sora","Sending get_friend_list request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetGroupList
            }, connection);
            //处理API返回信息
            int retCode = GetBaseRetCode(ret).retCode;
            ConsoleLog.Debug("Sora", $"Get get_friend_list response retcode={retCode}");
            if (retCode != 0 || ret["data"] == null) return new List<Group>();
            //处理返回群组列表
            return ret["data"].ToObject<List<Group>>();
        }
        #endregion

        #region 无回调API请求
        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">消息id</param>
        internal static async ValueTask DeleteMsg(Guid connection, int msgId)
        {
            ConsoleLog.Debug("Sora","Sending delete_msg request");
            await SendApiMessage(new ApiRequest
            {
                ApiType = APIType.DeleteMsg,
                ApiParams = new DeletMsgParams
                {
                    MessageId = msgId
                }
            }, connection);
        }
        #endregion

        #region API请求回调
        /// <summary>
        /// 获取到API响应
        /// </summary>
        /// <param name="echo">标识符</param>
        /// <param name="response">响应json</param>
        internal static void GetResponse(Guid echo, JObject response)
        {
            if (RequestList.Any(guid => guid == echo))
            {
                OnebotSubject.OnNext(Tuple.Create(echo, response));
                RequestList.Remove(echo);
            }
        }
        #endregion

        #region 发送API请求
        /// <summary>
        /// 向API客户端发送请求数据
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="connectionGuid">服务器连接标识符</param>
        /// <returns></returns>
        private static async ValueTask SendApiMessage(object message, Guid connectionGuid)
        {
            //向客户端发送请求数据
            if(!OnebotWSServer.ConnectionInfos.TryGetValue(connectionGuid, out IWebSocketConnection clientConnection)) return;
            await clientConnection.Send(JsonConvert.SerializeObject(message,Formatting.None));
        }

        /// <summary>
        /// 向API客户端发送请求数据
        /// </summary>
        /// <param name="message">信息</param>
        /// <param name="connectionGuid">服务器连接标识符</param>
        /// <returns>API返回</returns>
        private static async ValueTask<JObject> SendApiRequest(object message,Guid connectionGuid)
        {
            Guid echo = ((ApiRequest) message).Echo;
            //添加新的请求记录
            RequestList.Add(echo);
            //向客户端发送请求数据
            if(!OnebotWSServer.ConnectionInfos.TryGetValue(connectionGuid, out IWebSocketConnection clientConnection)) return null;
            await clientConnection.Send(JsonConvert.SerializeObject(message,Formatting.None));
            try
            {
                //等待客户端返回调用结果
                JObject response = await OnebotSubject
                                         .Where(ret => ret.Item1 == echo)
                                         .Select(ret => ret.Item2)
                                         .Take(1).Timeout(TimeSpan.FromMilliseconds(TimeOut))
                                         .Catch(Observable.Return<JObject>(null)).ToTask();
                if(response == null) ConsoleLog.Debug("Sora","API Time Out");
                return response;
            }
            catch (TimeoutException e)
            {
                //超时错误
                ConsoleLog.Error("Sora",$"API客户端请求超时({e.Message})");
                return null;
            }
        }
        #endregion

        #region 获取API返回的状态值
        /// <summary>
        /// 获取API状态返回值
        /// 所有API回调请求都会返回状态值
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static (int retCode,string status) GetBaseRetCode(JObject msg)
        {
            if (msg == null) return (retCode: -1, status: "failed");
            return
            (
                retCode:int.TryParse(msg["retcode"]?.ToString(),out int messageCode) ? messageCode : -1,
                status:msg["status"]?.ToString() ?? "failed"
            );
        }
        #endregion
    }
}