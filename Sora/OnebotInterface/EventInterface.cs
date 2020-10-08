using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.MessageEvent;
using Sora.EventArgs.OnebotEvent.MetaEvent;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.EventArgs.OnebotEvent.RequestEvent;
using Sora.Model.CQCodes;
using Sora.Model.Message;
using Sora.Model.OnebotApi;
using Sora.Model.SoraModel;
using Sora.Tool;

namespace Sora.OnebotInterface
{
    /// <summary>
    /// Onebot事件接口
    /// 判断和分发基类事件
    /// </summary>
    public static class EventInterface
    {
        #region 静态记录表
        /// <summary>
        /// 心跳包记录
        /// </summary>
        internal static readonly Dictionary<Guid,long> HeartBeatList = new Dictionary<Guid, long>();
        #endregion

        #region 事件委托
        /// <summary>
        /// Onebot事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="sender">产生事件的客户端</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask OnebotAsyncCallBackHandler<in TEventArgs>(Guid sender, TEventArgs eventArgs)where TEventArgs : System.EventArgs;
        #endregion

        #region 事件分发
        /// <summary>
        /// 事件分发
        /// </summary>
        /// <param name="messageJson">消息json对象</param>
        /// <param name="connection">客户端链接接口</param>
        internal static void Adapter(JObject messageJson, Guid connection)
        {
            switch (GetBaseEventType(messageJson))
            {
                //元事件类型
                case "meta_event":
                    MetaAdapter(messageJson, connection);
                    break;
                case "message":
                    MessageAdapter(messageJson, connection);
                    break;
                case "request":
                    RequestAdapter(messageJson, connection);
                    break;
                case "notice":
                    NoticeAdapter(messageJson, connection);
                    break;
                default:
                    //尝试从响应中获取标识符
                    if (!messageJson.TryGetValue("echo", out JToken echoJson)||
                        !Guid.TryParse(echoJson.ToString(),out Guid echo)||
                        //查找请求标识符是否存在
                        !ApiInterface.RequestList.Any(e => e.Equals(echo))) 
                        return;

                    //取出返回值中的数据
                    ApiInterface.GetResponse(echo, messageJson);
                    break;
            }
        }
        #endregion

        #region 元事件处理和分发
        /// <summary>
        /// 元事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private static async void MetaAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMetaEventType(messageJson))
            {
                //心跳包
                case "heartbeat":
                    HeartBeatEventArgs heartBeat = messageJson.ToObject<HeartBeatEventArgs>();
                    //TODO 暂时禁用心跳Log
                    //ConsoleLog.Debug("Sora",$"Get hreatbeat from [{connection}]");
                    if (heartBeat != null)
                    {
                        //刷新心跳包记录
                        if (HeartBeatList.Any(conn => conn.Key == connection))
                        {
                            HeartBeatList[connection] = heartBeat.Time;
                        }
                        else
                        {
                            HeartBeatList.TryAdd(connection,heartBeat.Time);
                        }
                    }
                    break;
                //生命周期
                case "lifecycle":
                    LifeCycleEventArgs lifeCycle = messageJson.ToObject<LifeCycleEventArgs>();
                    if (lifeCycle != null) ConsoleLog.Debug("Sore", $"Lifecycle event[{lifeCycle.SubType}] from [{connection}]");
                    //未知原因会丢失第一次调用的返回值，直接丢弃第一次调用
                    await ApiInterface.GetOnebotVersion(connection);
                    (_, ClientType clientType, string clientVer) = await ApiInterface.GetOnebotVersion(connection);
                    ConsoleLog.Info("Sora",$"已连接到{Enum.GetName(clientType)}客户端,版本:{clientVer}");
                    break;
                default:
                    ConsoleLog.Warning("Sora",$"接收到未知事件[{GetMetaEventType(messageJson)}]");
                    break;
            }
        }
        #endregion

        #region 消息事件处理和分发
        /// <summary>
        /// 消息事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private static async void MessageAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMessageType(messageJson))
            {
                //私聊事件
                case "private":
                    PrivateMessageEventArgs privateMessage = messageJson.ToObject<PrivateMessageEventArgs>();
                    if(privateMessage == null) break;
                    ConsoleLog.Debug("Sora",$"Private msg {privateMessage.Sender.Nick}({privateMessage.UserId}) : {privateMessage.RawMessage}");
                    break;
                //群聊事件
                case "group":
                    GroupMessageEventArgs groupMessage = messageJson.ToObject<GroupMessageEventArgs>();
                    if(groupMessage == null) break;
                    ConsoleLog.Debug("Sora",$"Group msg[{groupMessage.GroupId}] form {groupMessage.Sender.Nick}[{groupMessage.UserId}] : {groupMessage.RawMessage}");

                    #region 暂时的测试区域
                    List<CQCode> msg_g    = new List<CQCode>();
                    msg_g.Add(CQCode.CQText("哇哦"));
                    var test = await ApiInterface.SendGroupMessage(connection, 883740678, msg_g);
                    List<FriendInfo> friendList = await ApiInterface.GetFriendList(connection);
                    #endregion

                    break;
                default:
                    ConsoleLog.Warning("Sora",$"接收到未知事件[{GetMessageType(messageJson)}]");
                    break;
            }
        }
        #endregion

        #region 请求事件处理和分发
        /// <summary>
        /// 请求事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private static void RequestAdapter(JObject messageJson, Guid connection)
        {
            switch (GetRequestType(messageJson))
            {
                //好友请求事件
                case "friend":
                    FriendRequestEventArgs friendRequest = messageJson.ToObject<FriendRequestEventArgs>();
                    if(friendRequest == null)  break;
                    ConsoleLog.Debug("Sora",$"Friend request form [{friendRequest.UserId}] with commont[{friendRequest.Comment}]");
                    break;
                //群组请求事件
                case "group":
                    GroupRequestEventArgs groupRequest = messageJson.ToObject<GroupRequestEventArgs>();
                    if(groupRequest == null) break;
                    ConsoleLog.Debug("Sora",$"Group request [{groupRequest.SubType}] form [{groupRequest.UserId}] with commont[{groupRequest.Comment}] | flag[{groupRequest.Flag}]");
                    break;
                default:
                    ConsoleLog.Warning("Sora",$"接收到未知事件[{GetRequestType(messageJson)}]");
                    break;
            }
        }
        #endregion

        #region 通知事件处理和分发
        /// <summary>
        /// 通知事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private static void NoticeAdapter(JObject messageJson, Guid connection)
        {
            switch (GetNoticeType(messageJson))
            {
                //群文件上传
                case "group_upload":
                    FileUploadEventArgs fileUpload = messageJson.ToObject<FileUploadEventArgs>();
                    if(fileUpload == null) break;
                    ConsoleLog.Debug("Sora",
                                     $"Group notice[Upload file] file[{fileUpload.Upload.Name}] from group[{fileUpload.GroupId}({fileUpload.UserId})]");
                    break;
                //群管理员变动
                case "group_admin":
                    AdminChangeEventArgs adminChange = messageJson.ToObject<AdminChangeEventArgs>();
                    if(adminChange == null) break;
                    ConsoleLog.Debug("Sora",
                                     $"Group amdin change[{adminChange.SubType}] from group[{adminChange.GroupId}] by[{adminChange.UserId}]");
                    break;
                //群成员变动
                case "group_decrease":case "group_increase":
                    GroupMemberChangeEventArgs groupMemberChange = messageJson.ToObject<GroupMemberChangeEventArgs>();
                    if (groupMemberChange == null) break;
                    ConsoleLog.Debug("Sora",
                                     $"{groupMemberChange.NoticeType} type[{groupMemberChange.SubType}] member {groupMemberChange.GroupId}[{groupMemberChange.UserId}]");
                    break;
                //群禁言
                case "group_ban":
                    GroupBanEventArgs groupBan = messageJson.ToObject<GroupBanEventArgs>();
                    if (groupBan == null) break;
                    ConsoleLog.Debug("Sora",
                                     $"Group[{groupBan.GroupId}] {groupBan.SubType} member[{groupBan.UserId}]{groupBan.Duration}");
                    break;
                //好友添加
                case "friend_add":
                    FriendAddEventArgs friendAdd = messageJson.ToObject<FriendAddEventArgs>();
                    if(friendAdd == null) break;
                    ConsoleLog.Debug("Sora",$"Friend add user[{friendAdd.UserId}]");
                    break;
                //群消息撤回
                case "group_recall":
                    GroupRecallEventArgs groupRecall = messageJson.ToObject<GroupRecallEventArgs>();
                    if(groupRecall == null) break;
                    ConsoleLog.Debug("Sora",
                                     $"Group[{groupRecall.GroupId}] recall by [{groupRecall.OperatorId}],msg id={groupRecall.MessageId} sender={groupRecall.UserId}");
                    break;
                //好友消息撤回
                case "friend_recall":
                    FriendRecallEventArgs friendRecall = messageJson.ToObject<FriendRecallEventArgs>();
                    if(friendRecall == null) break;
                    ConsoleLog.Debug("Sora", $"Friend[{friendRecall.UserId}] recall msg id={friendRecall.MessageId}");
                    break;
                //通知类事件
                case "notify":
                    switch (GetNotifyType(messageJson))
                    {
                        case "poke"://戳一戳
                            PokeOrLuckyEventArgs pokeEvent = messageJson.ToObject<PokeOrLuckyEventArgs>();
                            if(pokeEvent == null) break;
                            ConsoleLog.Debug("Sora",
                                             $"Group[{pokeEvent.GroupId}] poke from [{pokeEvent.UserId}] to [{pokeEvent.TargetId}]");
                            break;
                        case "lucky_king"://运气王
                            PokeOrLuckyEventArgs luckyEvent = messageJson.ToObject<PokeOrLuckyEventArgs>();
                            if(luckyEvent == null) break;
                            ConsoleLog.Debug("Sora",
                                             $"Group[{luckyEvent.GroupId}] lucky king user[{luckyEvent.TargetId}]");
                            break;
                        case "honor":
                            HonorEventArgs honorEvent = messageJson.ToObject<HonorEventArgs>();
                            if (honorEvent == null) break;
                            ConsoleLog.Debug("Sora",
                                             $"Group[{honorEvent.GroupId}] member honor change [{honorEvent.HonorType}]");
                            break;
                        default:
                            ConsoleLog.Warning("Sora",$"未知Notify事件类型[{GetNotifyType(messageJson)}]");
                            break;
                    }
                    break;
                default:
                    ConsoleLog.Warning("Sora",$"接收到未知事件[{GetNoticeType(messageJson)}]");
                    break;
            }
        }
        #endregion

        #region 事件类型获取
        /// <summary>
        /// 获取上报事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetBaseEventType(JObject messageJson) =>
            !messageJson.TryGetValue("post_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        /// <summary>
        /// 获取元事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetMetaEventType(JObject messageJson) =>
            !messageJson.TryGetValue("meta_event_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        /// <summary>
        /// 获取消息事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetMessageType(JObject messageJson) =>
            !messageJson.TryGetValue("message_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        /// <summary>
        /// 获取请求事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetRequestType(JObject messageJson) =>
            !messageJson.TryGetValue("request_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        /// <summary>
        /// 获取通知事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetNoticeType(JObject messageJson) =>
            !messageJson.TryGetValue("notice_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        /// <summary>
        /// 获取通知事件子类型
        /// </summary>
        /// <param name="messageJson"></param>
        private static string GetNotifyType(JObject messageJson) =>
            !messageJson.TryGetValue("sub_type", out JToken typeJson) ? string.Empty : typeJson.ToString();

        #endregion
    }
}
