using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Command;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using Sora.Net;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using Sora.OnebotModel.OnebotEvent.MetaEvent;
using Sora.OnebotModel.OnebotEvent.NoticeEvent;
using Sora.OnebotModel.OnebotEvent.RequestEvent;
using YukariToolBox.LightLog;

namespace Sora.OnebotAdapter;

/// <summary>
/// Onebot事件接口
/// 判断和分发基类事件
/// </summary>
public sealed class EventAdapter
{
    #region 私有字段

    private readonly CommandManager _commandManager;

    #endregion

    #region 属性

    /// <summary>
    /// 特性指令管理器
    /// </summary>
    public CommandManager CommandManager
    {
        private init => _commandManager = value;
        get
        {
            if (StaticVariable.ServiceConfigs[ServiceId].EnableSoraCommandManager) return _commandManager;
            var e = new InvalidOperationException("在禁用指令管理器后尝试调用管理器，请在开启指令服务后再调用此实例");
            Log.Fatal(e, "非法操作", "指令服务已被禁用");
            throw e;
        }
    }

    /// <summary>
    /// 服务ID
    /// </summary>
    private Guid ServiceId { get; }

    #endregion

    #region 构造方法

    internal EventAdapter(Guid serviceId)
    {
        ServiceId = serviceId;
        CommandManager = StaticVariable.ServiceConfigs[serviceId].EnableSoraCommandManager
            ? new CommandManager(Assembly.GetEntryAssembly())
            : null;
    }

    #endregion

    #region 事件委托

    /// <summary>
    /// Onebot事件回调
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数</typeparam>
    /// <param name="eventType">事件的主要类型</param>
    /// <param name="eventArgs">事件参数</param>
    public delegate ValueTask EventAsyncCallBackHandler<in TEventArgs>(string eventType, TEventArgs eventArgs)
        where TEventArgs : System.EventArgs;

    #endregion

    #region 事件回调

    /// <summary>
    /// onebot链接完成事件
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
    /// 群成员头衔更新
    /// </summary>
    public event EventAsyncCallBackHandler<TitleUpdateEventArgs> OnTitleUpdate;

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
        if (StaticVariable.ServiceConfigs[ServiceId].EnableSocketMessage)
            Log.Debug("Socket",
                $"Get json message:{messageJson.ToString(Formatting.None)}");
        switch (TryGetJsonValue(messageJson, "post_type"))
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
                if (messageJson.TryGetValue("echo", out JToken echoJson) &&
                    Guid.TryParse(echoJson.ToString(), out Guid echo))
                    //取出返回值中的数据
                    ReactiveApiManager.GetResponse(echo, messageJson);
                else
                    Log.Warning("Sora",
                        $"Unknown message type:{TryGetJsonValue(messageJson, "post_type")}\r\njson = {messageJson}");

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
        switch (TryGetJsonValue(messageJson, "meta_event_type"))
        {
            //心跳包
            case "heartbeat":
            {
                var heartBeat = messageJson.ToObject<OnebotHeartBeatEventArgs>();
                Log.Debug("Sora", $"Get heartbeat from [{connection}]");
                //刷新心跳包记录
                if (heartBeat != null)
                    ConnectionManager.HeartBeatUpdate(connection);
                break;
            }
            //生命周期
            case "lifecycle":
            {
                var lifeCycle = messageJson.ToObject<OnebotLifeCycleEventArgs>();
                if (lifeCycle != null)
                    Log.Debug("Sore", $"Lifecycle event[{lifeCycle.SubType}] from [{connection}]");

                (ApiStatus infoApiStatus, string clientType, string clientVer) =
                    await ApiAdapter.GetClientInfo(connection);
                if (infoApiStatus.RetCode != ApiStatusType.Ok) //检查返回值
                {
                    Log.Error("Sora", $"获取onebot版本失败(retcode={infoApiStatus})");
                    break;
                }

                (ApiStatus loginInfoApiStatus, long uid, _) = await ApiAdapter.GetLoginInfo(connection);
                if (loginInfoApiStatus.RetCode != ApiStatusType.Ok) //检查返回值
                {
                    Log.Error("Sora", $"获取uid失败(retcode={loginInfoApiStatus})");
                    break;
                }

                ConnectionManager.UpdateUid(connection, uid);

                Log.Info("Sora", $"已连接到{clientType},版本:{clientVer}");

                if (OnClientConnect == null) break;
                //执行回调
                await OnClientConnect("Meta Event",
                    new ConnectEventArgs(ServiceId, connection, "lifecycle",
                        lifeCycle?.SelfId ?? -1, clientType, clientVer,
                        lifeCycle?.Time   ?? 0));
                break;
            }
            default:
                Log.Warning("Sora|MetaEvent", $"接收到未知事件[{TryGetJsonValue(messageJson, "meta_event_type")}]");
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
        switch (TryGetJsonValue(messageJson, "message_type"))
        {
            //私聊事件
            case "private":
            {
                var privateMsg = messageJson.ToObject<OnebotPrivateMsgEventArgs>();
                if (privateMsg == null) break;
                //检查屏蔽用户
                if (StaticVariable.ServiceConfigs[ServiceId].BlockUsers.Contains(privateMsg.UserId)) return;
                Log.Debug("Sora",
                    $"Private msg {privateMsg.SenderInfo.Nick}({privateMsg.UserId}) <- {privateMsg.RawMessage}");
                var eventArgs = new PrivateMessageEventArgs(ServiceId, connection, "private", privateMsg);
                //标记消息已读
                if (StaticVariable.ServiceConfigs[ServiceId].AutoMarkMessageRead)
                    ApiAdapter.InternalMarkMessageRead(connection, privateMsg.MessageId);
                //处理指令
                if (StaticVariable.ServiceConfigs[ServiceId].EnableSoraCommandManager)
                    await CommandManager.CommandAdapter(eventArgs);
                if (!eventArgs.IsContinueEventChain)
                    break;
                //执行回调
                if (OnPrivateMessage == null) break;
                await OnPrivateMessage("Message", eventArgs);
                break;
            }
            //群聊事件
            case "group":
            {
                var groupMsg = messageJson.ToObject<OnebotGroupMsgEventArgs>();
                if (groupMsg == null) break;
                if (StaticVariable.ServiceConfigs[ServiceId].BlockUsers.Contains(groupMsg.UserId)) return;
                Log.Debug("Sora",
                    $"Group msg[{groupMsg.GroupId}] form {groupMsg.SenderInfo.Nick}[{groupMsg.UserId}] <- {groupMsg.RawMessage}");
                var eventArgs = new GroupMessageEventArgs(ServiceId, connection, "group", groupMsg);
                //标记消息已读
                if (StaticVariable.ServiceConfigs[ServiceId].AutoMarkMessageRead)
                    ApiAdapter.InternalMarkMessageRead(connection, groupMsg.MessageId);
                //处理指令
                if (StaticVariable.ServiceConfigs[ServiceId].EnableSoraCommandManager)
                    await CommandManager.CommandAdapter(eventArgs);
                if (!eventArgs.IsContinueEventChain)
                    break;
                //执行回调
                if (OnGroupMessage == null) break;
                await OnGroupMessage("Message", eventArgs);
                break;
            }
            default:
                Log.Warning("Sora|Message", $"接收到未知事件[{TryGetJsonValue(messageJson, "message_type")}]");
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
        switch (TryGetJsonValue(messageJson, "message_type"))
        {
            case "group":
            {
                var groupMsg = messageJson.ToObject<OnebotGroupMsgEventArgs>();
                if (groupMsg == null) break;
                Log.Debug("Sora",
                    $"Group self msg[{groupMsg.GroupId}] -> {groupMsg.RawMessage}");
                //执行回调
                if (OnSelfMessage == null) break;
                await OnSelfMessage("Message",
                    new GroupMessageEventArgs(ServiceId, connection, "group", groupMsg));
                break;
            }
            default:
                Log.Warning("Sora|Message", $"接收到未知事件[{TryGetJsonValue(messageJson, "message_type")}]");
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
        switch (TryGetJsonValue(messageJson, "request_type"))
        {
            //好友请求事件
            case "friend":
            {
                var friendRequest = messageJson.ToObject<OnebotFriendRequestEventArgs>();
                if (friendRequest == null) break;
                Log.Debug("Sora",
                    $"Friend request form [{friendRequest.UserId}] with comment[{friendRequest.Comment}] | flag[{friendRequest.Flag}]");
                //执行回调
                if (OnFriendRequest == null) break;
                await OnFriendRequest("Request",
                    new FriendRequestEventArgs(ServiceId, connection, "request|friend",
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

                var groupRequest = messageJson.ToObject<OnebotGroupRequestEventArgs>();
                if (groupRequest == null) break;
                Log.Debug("Sora",
                    $"Group request [{groupRequest.GroupRequestType}] form [{groupRequest.UserId}] with commont[{groupRequest.Comment}] | flag[{groupRequest.Flag}]");
                //执行回调
                if (OnGroupRequest == null) break;
                await OnGroupRequest("Request",
                    new AddGroupRequestEventArgs(ServiceId, connection, "request|group",
                        groupRequest));
                break;
            }
            default:
                Log.Warning("Sora|Request", $"接收到未知事件[{TryGetJsonValue(messageJson, "request_type")}]");
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
        switch (TryGetJsonValue(messageJson, "notice_type"))
        {
            //群文件上传
            case "group_upload":
            {
                var fileUpload = messageJson.ToObject<OnebotFileUploadEventArgs>();
                if (fileUpload == null) break;
                Log.Debug("Sora",
                    $"Group notice[Upload file] file[{fileUpload.Upload.Name}] from group[{fileUpload.GroupId}({fileUpload.UserId})]");
                //执行回调
                if (OnFileUpload == null) break;
                await OnFileUpload("Notice",
                    new FileUploadEventArgs(ServiceId, connection, "group_upload", fileUpload));
                break;
            }
            //群管理员变动
            case "group_admin":
            {
                var adminChange = messageJson.ToObject<OnebotAdminChangeEventArgs>();
                if (adminChange == null) break;
                Log.Debug("Sora",
                    $"Group admin change[{adminChange.SubType}] from group[{adminChange.GroupId}] by[{adminChange.UserId}]");
                //执行回调
                if (OnGroupAdminChange == null) break;
                await OnGroupAdminChange("Notice",
                    new GroupAdminChangeEventArgs(ServiceId, connection, "group_upload",
                        adminChange));
                break;
            }
            //群成员变动
            case "group_decrease":
            case "group_increase":
            {
                var groupMemberChange = messageJson.ToObject<OnebotGroupMemberChangeEventArgs>();
                if (groupMemberChange == null) break;
                Log.Debug("Sora",
                    $"{groupMemberChange.NoticeType} type[{groupMemberChange.SubType}] member {groupMemberChange.GroupId}[{groupMemberChange.UserId}]");
                //执行回调
                if (OnGroupMemberChange == null) break;
                await OnGroupMemberChange("Notice",
                    new GroupMemberChangeEventArgs(ServiceId, connection,
                        "group_member_change",
                        groupMemberChange));
                break;
            }
            //群禁言
            case "group_ban":
            {
                var groupMute = messageJson.ToObject<OnebotGroupMuteEventArgs>();
                if (groupMute == null) break;
                Log.Debug("Sora",
                    $"Group[{groupMute.GroupId}] {groupMute.ActionType} member[{groupMute.UserId}]{groupMute.Duration}");
                //执行回调
                if (OnGroupMemberMute == null) break;
                await OnGroupMemberMute("Notice",
                    new GroupMuteEventArgs(ServiceId, connection, "group_ban", groupMute));
                break;
            }
            //好友添加
            case "friend_add":
            {
                var friendAdd = messageJson.ToObject<OnebotFriendAddEventArgs>();
                if (friendAdd == null) break;
                Log.Debug("Sora", $"Friend add user[{friendAdd.UserId}]");
                //执行回调
                if (OnFriendAdd == null) break;
                await OnFriendAdd("Notice",
                    new FriendAddEventArgs(ServiceId, connection, "friend_add", friendAdd));
                break;
            }
            //群消息撤回
            case "group_recall":
            {
                var groupRecall = messageJson.ToObject<ApiGroupRecallEventArgs>();
                if (groupRecall == null) break;
                Log.Debug("Sora",
                    $"Group[{groupRecall.GroupId}] recall by [{groupRecall.OperatorId}],msg id={groupRecall.MessageId} sender={groupRecall.UserId}");
                //执行回调
                if (OnGroupRecall == null) break;
                await OnGroupRecall("Notice",
                    new GroupRecallEventArgs(ServiceId, connection, "group_recall", groupRecall));
                break;
            }
            //好友消息撤回
            case "friend_recall":
            {
                var friendRecall = messageJson.ToObject<OnebotFriendRecallEventArgs>();
                if (friendRecall == null) break;
                Log.Debug("Sora", $"Friend[{friendRecall.UserId}] recall msg id={friendRecall.MessageId}");
                //执行回调
                if (OnFriendRecall == null) break;
                await OnFriendRecall("Notice",
                    new FriendRecallEventArgs(ServiceId, connection, "friend_recall",
                        friendRecall));
                break;
            }
            //群名片变更
            //仅支持GoCQ
            case "group_card":
            {
                var groupCardUpdate =
                    messageJson.ToObject<OnebotGroupCardUpdateEventArgs>();
                if (groupCardUpdate == null) break;
                Log.Debug("Sora",
                    $"Group[{groupCardUpdate.GroupId}] member[{groupCardUpdate.UserId}] card update [{groupCardUpdate.OldCard} => {groupCardUpdate.NewCard}]");
                if (OnGroupCardUpdate == null) break;
                await OnGroupCardUpdate("Notice",
                    new GroupCardUpdateEventArgs(ServiceId, connection, "group_card",
                        groupCardUpdate));
                break;
            }
            case "offline_file":
            {
                var offlineFile = messageJson.ToObject<OnebotOfflineFileEventArgs>();
                if (offlineFile == null) break;
                Log.Debug("Sora",
                    $"Get offline file from[{offlineFile.UserId}] file name = {offlineFile.Info.Name}");
                if (OnOfflineFileEvent == null) break;
                await OnOfflineFileEvent("Notice",
                    new OfflineFileEventArgs(ServiceId, connection, "offline_file",
                        offlineFile));
                break;
            }
            case "client_status":
            {
                var clientStatus = messageJson.ToObject<OnebotClientStatusEventArgs>();
                if (clientStatus == null) break;
                Log.Debug("Sora",
                    $"Get client status change from[{clientStatus.UserId}] client id = {clientStatus.ClientInfo.AppId}");
                if (OnClientStatusChangeEvent == null) break;
                await OnClientStatusChangeEvent("Notice",
                    new ClientStatusChangeEventArgs(ServiceId, connection,
                        "client_status",
                        clientStatus));
                break;
            }
            case "essence":
            {
                var essenceChange = messageJson.ToObject<OnebotEssenceChangeEventArgs>();
                if (essenceChange == null) break;
                Log.Debug("Sora",
                    $"Get essence change msg_id = {essenceChange.MessageId} type = {essenceChange.EssenceChangeType}");
                if (OnEssenceChange == null) break;
                await OnEssenceChange("Notice",
                    new EssenceChangeEventArgs(ServiceId, connection, "essence", essenceChange));
                break;
            }
            //通知类事件
            case "notify":
                switch (TryGetJsonValue(messageJson, "sub_type"))
                {
                    case "poke": //戳一戳
                    {
                        var pokeEvent = messageJson.ToObject<OnebotPokeOrLuckyEventArgs>();
                        if (pokeEvent == null) break;
                        Log.Debug("Sora",
                            $"Group[{pokeEvent.GroupId}] poke from [{pokeEvent.UserId}] to [{pokeEvent.TargetId}]");
                        if (OnGroupPoke == null) break;
                        await OnGroupPoke("Notify",
                            new GroupPokeEventArgs(ServiceId, connection, "poke", pokeEvent));
                        break;
                    }
                    case "lucky_king": //运气王
                    {
                        var luckyEvent = messageJson.ToObject<OnebotPokeOrLuckyEventArgs>();
                        if (luckyEvent == null) break;
                        Log.Debug("Sora",
                            $"Group[{luckyEvent.GroupId}] lucky king user[{luckyEvent.TargetId}]");
                        if (OnLuckyKingEvent == null) break;
                        await OnLuckyKingEvent("Notify",
                            new LuckyKingEventArgs(ServiceId, connection, "lucky_king",
                                luckyEvent));
                        break;
                    }
                    case "honor":
                    {
                        var honorEvent = messageJson.ToObject<OnebotHonorEventArgs>();
                        if (honorEvent == null) break;
                        Log.Debug("Sora",
                            $"Group[{honorEvent.GroupId}] member honor change [{honorEvent.HonorType}]");
                        if (OnHonorEvent == null) break;
                        await OnHonorEvent("Notify",
                            new HonorEventArgs(ServiceId, connection, "honor", honorEvent));
                        break;
                    }
                    case "title":
                    {
                        var newTitleEvent = messageJson.ToObject<OnebotMemberTitleUpdatedEventArgs>();
                        if (newTitleEvent == null) break;
                        Log.Debug("Sora",
                            $"Group[{newTitleEvent.GroupId} member title change to [{newTitleEvent.NewTitle}]]");
                        if (OnTitleUpdate == null) break;
                        await OnTitleUpdate("Notify",
                            new TitleUpdateEventArgs(ServiceId, connection, "title",
                                newTitleEvent));
                        break;
                    }
                    default:
                        Log.Warning("Sora|Notify", $"接收到未知事件[{TryGetJsonValue(messageJson, "sub_type")}]");
                        break;
                }

                break;
            default:
                Log.Warning("Sora|Notice", $"接收到未知事件[{TryGetJsonValue(messageJson, "notice_type")}]");
                break;
        }
    }

    #endregion

    #region 事件类型获取

    /// <summary>
    /// 获取json中的值
    /// </summary>
    private static string TryGetJsonValue(JObject dataJson, string key)
    {
        return dataJson.TryGetValue(key, out JToken value) ? value.ToString() : string.Empty;
    }

    #endregion
}