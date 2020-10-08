using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Enumeration;
using Sora.Enumeration.ApiEnum;
using Sora.Model.OnebotApi.Request;
using Sora.Model.CQCodes;
using Sora.Model.CQCodes.CQCodeModel;
using Sora.Model.Message;
using Sora.Model.OnebotApi;
using Sora.Tool;

namespace Sora.OnebotInterface
{
    internal static class RequestApiInterface
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

        #region API请求信息序列化
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="connection">服务器连接</param>
        /// <param name="target">发送目标uid</param>
        /// <param name="messages">发送的信息</param>
        /// <returns>
        /// message id
        /// </returns>
        internal static async ValueTask<ApiResponseCollection> SendPrivateMessage(Guid connection, long target, List<CQCode> messages)
        {
            ConsoleLog.Debug("Sora", "Sending SendPrivateMessage request");
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
            ApiResponseCollection response = GetBaseRetCode(ret);
            if (response.RetCode != 0) return response;
            response.MessageId = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode)
                ? messageCode
                : -1;
            ConsoleLog.Debug("Sora", $"Get SendPrivateMessage response retcode={response.RetCode}|msg_id={response.MessageId}");
            return response;
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="connection">服务器连接</param>
        /// <param name="target">发送目标gid</param>
        /// <param name="messages">发送的信息</param>
        /// <returns>
        /// ApiResponseCollection
        /// </returns>
        internal static async ValueTask<ApiResponseCollection> SendGroupMessage(Guid connection, long target, List<CQCode> messages)
        {
            ConsoleLog.Debug("Sora", "Sending SendGroupMessage request");
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
            ApiResponseCollection response = GetBaseRetCode(ret);
            if (response.RetCode != 0) return response;
            response.MessageId = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode)
                ? messageCode
                : -1;
            ConsoleLog.Debug("Sora", $"Get SendGroupMessage response retcode={response.RetCode}|msg_id={response.MessageId}");
            return response;
        }

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="connection">服务器连接</param>
        /// <param name="msgId">合并转发 ID</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<ApiResponseCollection> GetForwardMessage(Guid connection, string msgId)
        {
            if(string.IsNullOrEmpty(msgId)) throw new NullReferenceException(nameof(msgId));
            ConsoleLog.Debug("Sora", "Sending GetForwardMessage request");
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
            ApiResponseCollection response = GetBaseRetCode(ret);
            ConsoleLog.Debug("Sora", ret?["data"]);
            if (response.RetCode != 0) return response;
            //转换消息类型
            NodeArray messageList =
                ret?["data"]?.ToObject<NodeArray>() ?? new NodeArray();
            messageList.ParseNode();
            response.NodeMessages = messageList;
            return response;
        }

        /// <summary>
        /// 获取登陆账号信息
        /// </summary>
        /// <param name="connection">服务器连接</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<ApiResponseCollection> GetLoginInfo(Guid connection)
        {
            ConsoleLog.Debug("Sora", "Sending GetLoginInfo request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetLoginInfo
            }, connection);
            //处理API返回信息
            ApiResponseCollection response = GetBaseRetCode(ret);
            ConsoleLog.Debug("Sora", $"Get GetLoginInfo response retcode={response.RetCode}");
            if (response.RetCode != 0) return response;
            response.Uid  = int.TryParse(ret["data"]?["user_id"]?.ToString(), out int messageCode) ? messageCode : -1;
            response.Nick = ret["data"]?["nickname"]?.ToString() ?? string.Empty;
            return response;
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="connection">服务器连接</param>
        internal static async ValueTask<ApiResponseCollection> GetOnebotVersion(Guid connection)
        {
            ConsoleLog.Debug("Sora", "Sending GetOnebotVersion request");
            JObject ret = await SendApiRequest(new ApiRequest
            {
                ApiType = APIType.GetVersion
            }, connection);
            //处理API返回信息
            ApiResponseCollection response = GetBaseRetCode(ret);
            ConsoleLog.Debug("Sora", $"Get GetOnebotVersion response retcode={response.RetCode}");
            if (response.RetCode != 0 || ret["data"] == null) return response;
            //判断是否为MiraiGo
            JObject.FromObject(ret["data"]).TryGetValue("go-cqhttp", out JToken clientJson);
            bool.TryParse(clientJson?.ToString() ?? "false", out bool isGo);
            if (isGo)
            {
                response.Client = ClientType.GoCqhttp;
            }
            response.ClientVer = ret["data"]?["version"]?.ToString() ?? string.Empty;
            return response;
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
        /// <param name="connection">服务器连接标识符</param>
        /// <returns>API返回</returns>
        private static async Task<JObject> SendApiRequest(object message,Guid connection)
        {
            Guid echo = ((ApiRequest) message).Echo;
            //添加新的请求记录
            RequestList.Add(echo);
            //向客户端发送请求数据
            await Task.Run(() =>
                           {
                               OnebotWSServer.ConnectionInfos[connection].Send(JsonConvert.SerializeObject(message,Formatting.Indented));
                           });
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
        /// <returns>ApiResponseCollection</returns>
        private static ApiResponseCollection GetBaseRetCode(JObject msg)
        {
            if (msg == null) return new ApiResponseCollection();
            return new ApiResponseCollection
            {
                RetCode = int.TryParse(msg["retcode"]?.ToString(),out int messageCode) ? messageCode : -1,
                Status  = msg["status"]?.ToString() ?? "failed"
            };
        }
        #endregion
    }
}