using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Sora.Command;
using Sora.EventArgs.SoraEvent;
using Sora.Net;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using Sora.OnebotModel.OnebotEvent.MetaEvent;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;
using Sora.OnebotModel.OnebotEvent.RequestEvent;
using YukariToolBox.FormatLog;

namespace Sora.OnebotInterface
{
    /// <summary>
    /// Onebot事件接口
    /// 判断和分发基类事件
    /// </summary>
    public class EventInterface
    {
        #region 属性

        /// <summary>
        /// 特性指令管理器
        /// </summary>
        private CommandManager CommandManager { get; set; }

        #endregion

        #region 构造方法

        internal EventInterface(bool enableSoraCommandManager)
        {
            CommandManager = new CommandManager(enableSoraCommandManager);
            CommandManager.MappingCommands(Assembly.GetEntryAssembly());
        }

        #endregion

        #region 事件委托

        /// <summary>
        /// Onebot事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="type">事件的主要类型</param>
        /// <param name="eventArgs">事件参数</param>
        public delegate ValueTask EventAsyncCallBackHandler<in TEventArgs>(string type, TEventArgs eventArgs)
            where TEventArgs : System.EventArgs;

        #endregion

        #region 事件回调

        /// <summary>
        /// 客户端链接完成事件
        /// </summary>
        public event EventAsyncCallBackHandler<ConnectEventArgs> OnClientConnect;

        /// <summary>
        /// 群聊事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupMessageEventArgs> OnGroupMessage;

        /// <summary>
        /// 登录账号发送消息事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupMessageEventArgs> OnSelfMessage;

        /// <summary>
        /// 私聊事件
        /// </summary>
        public event EventAsyncCallBackHandler<PrivateMessageEventArgs> OnPrivateMessage;

        /// <summary>
        /// 群申请事件
        /// </summary>
        public event EventAsyncCallBackHandler<AddGroupRequestEventArgs> OnGroupRequest;

        /// <summary>
        /// 好友申请事件
        /// </summary>
        public event EventAsyncCallBackHandler<FriendRequestEventArgs> OnFriendRequest;

        /// <summary>
        /// 群文件上传事件
        /// </summary>
        public event EventAsyncCallBackHandler<FileUploadEventArgs> OnFileUpload;

        /// <summary>
        /// 管理员变动事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupAdminChangeEventArgs> OnGroupAdminChange;

        /// <summary>
        /// 群成员变动事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupMemberChangeEventArgs> OnGroupMemberChange;

        /// <summary>
        /// 群成员禁言事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupMuteEventArgs> OnGroupMemberMute;

        /// <summary>
        /// 好友添加事件
        /// </summary>
        public event EventAsyncCallBackHandler<FriendAddEventArgs> OnFriendAdd;

        /// <summary>
        /// 群聊撤回事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupRecallEventArgs> OnGroupRecall;

        /// <summary>
        /// 好友撤回事件
        /// </summary>
        public event EventAsyncCallBackHandler<FriendRecallEventArgs> OnFriendRecall;

        /// <summary>
        /// 群名片变更事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupCardUpdateEventArgs> OnGroupCardUpdate;

        /// <summary>
        /// 群内戳一戳事件
        /// </summary>
        public event EventAsyncCallBackHandler<GroupPokeEventArgs> OnGroupPoke;

        /// <summary>
        /// 运气王事件
        /// </summary>
        public event EventAsyncCallBackHandler<LuckyKingEventArgs> OnLuckyKingEvent;

        /// <summary>
        /// 群成员荣誉变更事件
        /// </summary>
        public event EventAsyncCallBackHandler<HonorEventArgs> OnHonorEvent;

        /// <summary>
        /// 离线文件事件
        /// </summary>
        public event EventAsyncCallBackHandler<OfflineFileEventArgs> OnOfflineFileEvent;

        /// <summary>
        /// 其他客户端在线状态变更事件
        /// </summary>
        public event EventAsyncCallBackHandler<ClientStatusChangeEventArgs> OnClientStatusChangeEvent;

        /// <summary>
        /// 精华消息变动事件
        /// </summary>
        public event EventAsyncCallBackHandler<EssenceChangeEventArgs> OnEssenceChange;

        #endregion

        #region 事件分发

        /// <summary>
        /// 事件分发
        /// </summary>
        /// <param name="messageJson">消息json对象</param>
        /// <param name="connection">客户端链接接口</param>
        internal void Adapter(JObject messageJson, Guid connection)
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
                case "message_sent":
                    SelfMessageAdapter(messageJson, connection);
                    break;
                default:
                    //尝试从响应中获取标识符
                    if (messageJson.TryGetValue("echo", out var echoJson) &&
                        Guid.TryParse(echoJson.ToString(), out var echo))
                    {
                        //取出返回值中的数据
                        ReactiveApiManager.GetResponse(echo, messageJson);
                    }
                    else Log.Warning("Sora", $"Unknown message type:{GetBaseEventType(messageJson)}");

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
        private async void MetaAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMetaEventType(messageJson))
            {
                //心跳包
                case "heartbeat":
                {
                    ApiHeartBeatEventArgs heartBeat = messageJson.ToObject<ApiHeartBeatEventArgs>();
                    Log.Debug("Sora", $"Get heartbeat from [{connection}]");
                    //刷新心跳包记录
                    if (heartBeat != null)
                        ConnectionManager.HeartBeatUpdate(connection);
                    break;
                }
                //生命周期
                case "lifecycle":
                {
                    ApiLifeCycleEventArgs lifeCycle = messageJson.ToObject<ApiLifeCycleEventArgs>();
                    if (lifeCycle != null)
                        Log.Debug("Sore", $"Lifecycle event[{lifeCycle.SubType}] from [{connection}]");

                    var (retCode, clientType, clientVer) = await ApiInterface.GetClientInfo(connection);
                    if (retCode != 0) //检查返回值
                    {
                        Log.Error("Sora", $"获取onebot版本失败(retcode={retCode})");
                        break;
                    }

                    var (retCode2, uid, _) = await ApiInterface.GetLoginInfo(connection);
                    if (retCode2 != 0) //检查返回值
                    {
                        Log.Error("Sora", $"获取uid失败(retcode={retCode2})");
                        break;
                    }

                    ConnectionManager.UpdateUid(connection, uid);

                    Log.Info("Sora", $"已连接到{clientType},版本:{clientVer}");
                    if (OnClientConnect == null) break;
                    //执行回调
                    await OnClientConnect("Meta Event",
                                          new ConnectEventArgs(connection, "lifecycle",
                                                               lifeCycle?.SelfID ?? -1, clientType, clientVer,
                                                               lifeCycle?.Time   ?? 0));
                    break;
                }
                default:
                    Log.Warning("Sora|Meta Event", $"接收到未知事件[{GetMetaEventType(messageJson)}]");
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
        private async void MessageAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMessageType(messageJson))
            {
                //私聊事件
                case "private":
                {
                    var privateMsg = messageJson.ToObject<ApiPrivateMsgEventArgs>();
                    if (privateMsg == null) break;
                    Log.Debug("Sora",
                              $"Private msg {privateMsg.SenderInfo.Nick}({privateMsg.UserId}) <- {privateMsg.RawMessage}");
                    var eventArgs = new PrivateMessageEventArgs(connection, "private", privateMsg);
                    //处理指令
                    if (!await CommandManager.CommandAdapter(eventArgs))
                        break;
                    //执行回调
                    if (OnPrivateMessage == null) break;
                    await OnPrivateMessage("Message", eventArgs);
                    break;
                }
                //群聊事件
                case "group":
                {
                    ApiGroupMsgEventArgs groupMsg = messageJson.ToObject<ApiGroupMsgEventArgs>();
                    if (groupMsg == null) break;
                    Log.Debug("Sora",
                              $"Group msg[{groupMsg.GroupId}] form {groupMsg.SenderInfo.Nick}[{groupMsg.UserId}] <- {groupMsg.RawMessage}");
                    var eventArgs = new GroupMessageEventArgs(connection, "group", groupMsg);
                    //处理指令
                    if (!await CommandManager.CommandAdapter(eventArgs))
                        break;
                    //执行回调
                    if (OnGroupMessage == null) break;
                    await OnGroupMessage("Message", eventArgs);
                    break;
                }
                default:
                    Log.Warning("Sora|Message", $"接收到未知事件[{GetMessageType(messageJson)}]");
                    break;
            }
        }

        #endregion

        #region 自身消息事件处理和分发

        /// <summary>
        /// 自身事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private async void SelfMessageAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMessageType(messageJson))
            {
                case "group":
                {
                    ApiGroupMsgEventArgs groupMsg = messageJson.ToObject<ApiGroupMsgEventArgs>();
                    if (groupMsg == null) break;
                    Log.Debug("Sora",
                              $"Group self msg[{groupMsg.GroupId}] -> {groupMsg.RawMessage}");
                    //执行回调
                    if (OnSelfMessage == null) break;
                    await OnSelfMessage("Message",
                                        new GroupMessageEventArgs(connection, "group", groupMsg));
                    break;
                }
                default:
                    Log.Warning("Sora|Message", $"接收到未知事件[{GetMessageType(messageJson)}]");
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
        private async void RequestAdapter(JObject messageJson, Guid connection)
        {
            switch (GetRequestType(messageJson))
            {
                //好友请求事件
                case "friend":
                {
                    ApiFriendRequestEventArgs friendRequest = messageJson.ToObject<ApiFriendRequestEventArgs>();
                    if (friendRequest == null) break;
                    Log.Debug("Sora",
                              $"Friend request form [{friendRequest.UserId}] with commont[{friendRequest.Comment}] | flag[{friendRequest.Flag}]");
                    //执行回调
                    if (OnFriendRequest == null) break;
                    await OnFriendRequest("Request",
                                          new FriendRequestEventArgs(connection, "request|friend",
                                                                     friendRequest));
                    break;
                }
                //群组请求事件
                case "group":
                {
                    if (messageJson.TryGetValue("sub_type", out JToken sub) && sub.ToString().Equals("notice"))
                    {
                        Log.Warning("Sora", "收到notice消息类型，不解析此类型消息");
                        break;
                    }

                    ApiGroupRequestEventArgs groupRequest = messageJson.ToObject<ApiGroupRequestEventArgs>();
                    if (groupRequest == null) break;
                    Log.Debug("Sora",
                              $"Group request [{groupRequest.GroupRequestType}] form [{groupRequest.UserId}] with commont[{groupRequest.Comment}] | flag[{groupRequest.Flag}]");
                    //执行回调
                    if (OnGroupRequest == null) break;
                    await OnGroupRequest("Request",
                                         new AddGroupRequestEventArgs(connection, "request|group",
                                                                      groupRequest));
                    break;
                }
                default:
                    Log.Warning("Sora|Request", $"接收到未知事件[{GetRequestType(messageJson)}]");
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
        private async void NoticeAdapter(JObject messageJson, Guid connection)
        {
            switch (GetNoticeType(messageJson))
            {
                //群文件上传
                case "group_upload":
                {
                    ApiFileUploadEventArgs fileUpload = messageJson.ToObject<ApiFileUploadEventArgs>();
                    if (fileUpload == null) break;
                    Log.Debug("Sora",
                              $"Group notice[Upload file] file[{fileUpload.Upload.Name}] from group[{fileUpload.GroupId}({fileUpload.UserId})]");
                    //执行回调
                    if (OnFileUpload == null) break;
                    await OnFileUpload("Notice",
                                       new FileUploadEventArgs(connection, "group_upload", fileUpload));
                    break;
                }
                //群管理员变动
                case "group_admin":
                {
                    ApiAdminChangeEventArgs adminChange = messageJson.ToObject<ApiAdminChangeEventArgs>();
                    if (adminChange == null) break;
                    Log.Debug("Sora",
                              $"Group amdin change[{adminChange.SubType}] from group[{adminChange.GroupId}] by[{adminChange.UserId}]");
                    //执行回调
                    if (OnGroupAdminChange == null) break;
                    await OnGroupAdminChange("Notice",
                                             new GroupAdminChangeEventArgs(connection, "group_upload", adminChange));
                    break;
                }
                //群成员变动
                case "group_decrease":
                case "group_increase":
                {
                    ApiGroupMemberChangeEventArgs groupMemberChange =
                        messageJson.ToObject<ApiGroupMemberChangeEventArgs>();
                    if (groupMemberChange == null) break;
                    Log.Debug("Sora",
                              $"{groupMemberChange.NoticeType} type[{groupMemberChange.SubType}] member {groupMemberChange.GroupId}[{groupMemberChange.UserId}]");
                    //执行回调
                    if (OnGroupMemberChange == null) break;
                    await OnGroupMemberChange("Notice",
                                              new GroupMemberChangeEventArgs(connection, "group_member_change",
                                                                             groupMemberChange));
                    break;
                }
                //群禁言
                case "group_ban":
                {
                    ApiGroupMuteEventArgs groupMute = messageJson.ToObject<ApiGroupMuteEventArgs>();
                    if (groupMute == null) break;
                    Log.Debug("Sora",
                              $"Group[{groupMute.GroupId}] {groupMute.ActionType} member[{groupMute.UserId}]{groupMute.Duration}");
                    //执行回调
                    if (OnGroupMemberMute == null) break;
                    await OnGroupMemberMute("Notice",
                                            new GroupMuteEventArgs(connection, "group_ban", groupMute));
                    break;
                }
                //好友添加
                case "friend_add":
                {
                    ApiFriendAddEventArgs friendAdd = messageJson.ToObject<ApiFriendAddEventArgs>();
                    if (friendAdd == null) break;
                    Log.Debug("Sora", $"Friend add user[{friendAdd.UserId}]");
                    //执行回调
                    if (OnFriendAdd == null) break;
                    await OnFriendAdd("Notice",
                                      new FriendAddEventArgs(connection, "friend_add", friendAdd));
                    break;
                }
                //群消息撤回
                case "group_recall":
                {
                    ApiGroupRecallEventArgs groupRecall = messageJson.ToObject<ApiGroupRecallEventArgs>();
                    if (groupRecall == null) break;
                    Log.Debug("Sora",
                              $"Group[{groupRecall.GroupId}] recall by [{groupRecall.OperatorId}],msg id={groupRecall.MessageId} sender={groupRecall.UserId}");
                    //执行回调
                    if (OnGroupRecall == null) break;
                    await OnGroupRecall("Notice",
                                        new GroupRecallEventArgs(connection, "group_recall", groupRecall));
                    break;
                }
                //好友消息撤回
                case "friend_recall":
                {
                    ApiFriendRecallEventArgs friendRecall = messageJson.ToObject<ApiFriendRecallEventArgs>();
                    if (friendRecall == null) break;
                    Log.Debug("Sora", $"Friend[{friendRecall.UserId}] recall msg id={friendRecall.MessageId}");
                    //执行回调
                    if (OnFriendRecall == null) break;
                    await OnFriendRecall("Notice",
                                         new FriendRecallEventArgs(connection, "friend_recall", friendRecall));
                    break;
                }
                //群名片变更
                //此事件仅在Go上存在
                case "group_card":
                {
                    ApiGroupCardUpdateEventArgs groupCardUpdate = messageJson.ToObject<ApiGroupCardUpdateEventArgs>();
                    if (groupCardUpdate == null) break;
                    Log.Debug("Sora",
                              $"Group[{groupCardUpdate.GroupId}] member[{groupCardUpdate.UserId}] card update [{groupCardUpdate.OldCard} => {groupCardUpdate.NewCard}]");
                    if (OnGroupCardUpdate == null) break;
                    await OnGroupCardUpdate("Notice",
                                            new GroupCardUpdateEventArgs(connection, "group_card", groupCardUpdate));
                    break;
                }
                case "offline_file":
                {
                    ApiOfflineFileEventArgs offlineFile = messageJson.ToObject<ApiOfflineFileEventArgs>();
                    if (offlineFile == null) break;
                    Log.Debug("Sora",
                              $"Get offline file from[{offlineFile.UserId}] file name = {offlineFile.Info.Name}");
                    if (OnOfflineFileEvent == null) break;
                    await OnOfflineFileEvent("Notice",
                                             new OfflineFileEventArgs(connection, "offline_file", offlineFile));
                    break;
                }
                case "client_status":
                {
                    ApiClientStatusEventArgs clientStatus = messageJson.ToObject<ApiClientStatusEventArgs>();
                    if (clientStatus == null) break;
                    Log.Debug("Sora",
                              $"Get client status change from[{clientStatus.UserId}] client id = {clientStatus.ClientInfo.AppId}");
                    if (OnClientStatusChangeEvent == null) break;
                    await OnClientStatusChangeEvent("Notice",
                                                    new ClientStatusChangeEventArgs(connection, "client_status",
                                                        clientStatus));
                    break;
                }
                case "essence":
                {
                    ApiEssenceChangeEventArgs essenceChange = messageJson.ToObject<ApiEssenceChangeEventArgs>();
                    if (essenceChange == null) break;
                    Log.Debug("Sora",
                              $"Get essence change msg_id = {essenceChange.MessageId} type = {essenceChange.EssenceChangeType}");
                    if (OnEssenceChange == null) break;
                    await OnEssenceChange("Notice",
                                          new EssenceChangeEventArgs(connection, "essence", essenceChange));
                    break;
                }
                //通知类事件
                case "notify":
                    switch (GetNotifyType(messageJson))
                    {
                        case "poke": //戳一戳
                        {
                            ApiPokeOrLuckyEventArgs pokeEvent = messageJson.ToObject<ApiPokeOrLuckyEventArgs>();
                            if (pokeEvent == null) break;
                            Log.Debug("Sora",
                                      $"Group[{pokeEvent.GroupId}] poke from [{pokeEvent.UserId}] to [{pokeEvent.TargetId}]");
                            if (OnGroupPoke == null) break;
                            await OnGroupPoke("Notify",
                                              new GroupPokeEventArgs(connection, "poke", pokeEvent));
                            break;
                        }
                        case "lucky_king": //运气王
                        {
                            ApiPokeOrLuckyEventArgs luckyEvent = messageJson.ToObject<ApiPokeOrLuckyEventArgs>();
                            if (luckyEvent == null) break;
                            Log.Debug("Sora",
                                      $"Group[{luckyEvent.GroupId}] lucky king user[{luckyEvent.TargetId}]");
                            if (OnLuckyKingEvent == null) break;
                            await OnLuckyKingEvent("Notify",
                                                   new LuckyKingEventArgs(connection, "lucky_king", luckyEvent));
                            break;
                        }
                        case "honor":
                        {
                            ApiHonorEventArgs honorEvent = messageJson.ToObject<ApiHonorEventArgs>();
                            if (honorEvent == null) break;
                            Log.Debug("Sora",
                                      $"Group[{honorEvent.GroupId}] member honor change [{honorEvent.HonorType}]");
                            if (OnHonorEvent == null) break;
                            await OnHonorEvent("Notify",
                                               new HonorEventArgs(connection, "honor", honorEvent));
                            break;
                        }
                        default:
                            Log.Warning("Sora|Notify", $"接收到未知事件[{GetNotifyType(messageJson)}]");
                            break;
                    }

                    break;
                default:
                    Log.Warning("Sora|Notice", $"接收到未知事件[{GetNoticeType(messageJson)}]");
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