using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Attributes;
using Sora.Converter;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Net;
using Sora.Net.Records;
using Sora.OnebotModel.ApiParams;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using YukariToolBox.LightLog;

namespace Sora.OnebotAdapter;

internal static class ApiAdapter
{
#region API请求

    /// <summary>
    /// 发送私聊消息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="target">发送目标uid</param>
    /// <param name="messages">发送的信息</param>
    /// <param name="groupId">临时会话来源群，非临时会话时为<see langword="null"/></param>
    /// <param name="timeout">本次调用的超时，为<see langword="null"/>时使用默认超时时</param>
    /// <returns>
    /// message id
    /// </returns>
    internal static async ValueTask<(ApiStatus apiStatus, int messageId)> SendPrivateMessage(
        Guid        connection,
        long        target,
        MessageBody messages,
        long?       groupId,
        TimeSpan?   timeout)
    {
        if (messages == null || messages.Count == 0)
            throw new NullReferenceException(nameof(messages));
        //转换消息段列表
        List<OnebotSegment> msgList = messages.Where(msg => msg.MessageType != SegmentType.Ignore)
                                              .Select(msg => msg.ToOnebotSegment())
                                              .ToList();
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SendMsg,
                                                        ApiParams = new SendMessageParams
                                                        {
                                                            MessageType = MessageType.Private,
                                                            UserId      = target,
                                                            Message     = msgList,
                                                            GroupId     = groupId
                                                        }
                                                    },
                                                    connection,
                                                    timeout);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, -1);
        int msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out int messageCode) ? messageCode : -1;
        Log.Debug("Sora", $"msg send -> private[{target}]");
        return (apiStatus, msgCode);
    }

    /// <summary>
    /// 发送群聊消息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="target">发送目标gid</param>
    /// <param name="messages">发送的信息</param>
    /// <param name="timeout">覆盖原有超时</param>
    /// <returns>
    /// ApiResponseCollection
    /// </returns>
    internal static async ValueTask<(ApiStatus apiStatus, int messageId)> SendGroupMessage(
        Guid        connection,
        long        target,
        MessageBody messages,
        TimeSpan?   timeout)
    {
        if (messages == null || messages.Count == 0)
            throw new NullReferenceException(nameof(messages));
        //转换消息段列表
        List<OnebotSegment> msgList = messages.Where(msg => msg.MessageType != SegmentType.Ignore)
                                              .Select(msg => msg.ToOnebotSegment())
                                              .ToList();
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SendMsg,
                                                        ApiParams = new SendMessageParams
                                                        {
                                                            MessageType = MessageType.Group,
                                                            GroupId     = target,
                                                            Message     = msgList
                                                        }
                                                    },
                                                    connection,
                                                    timeout);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, -1);
        int msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out int messageCode) ? messageCode : -1;
        Log.Debug("Sora", $"msg send -> group[{target}]");
        return (apiStatus, msgCode);
    }

    /// <summary>
    /// 获取版本信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    internal static async ValueTask<(ApiStatus apiStatus, List<GroupNoticeInfo> noticeInfos)> GetGroupNotice(
        Guid connection,
        long groupId)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupNotice,
                                                        ApiParams      = new { group_id = groupId }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);

        return (apiStatus, ret["data"]?.ToObject<List<GroupNoticeInfo>>() ?? new List<GroupNoticeInfo>());
    }

    /// <summary>
    /// 获取登陆账号信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <returns>ApiResponseCollection</returns>
    [Reviewed("nidbCN", "2021-03-24 20:38")]
    internal static async ValueTask<(ApiStatus apiStatus, long userId, string nick)> GetLoginInfo(Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.GetLoginInfo },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, -1, null);
        return (apiStatus, userId: long.TryParse(ret?["data"]?["user_id"]?.ToString(), out long userId) ? userId : -1,
            nick: ret?["data"]?["nickname"]?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// 获取版本信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    [Reviewed("nidbCN", "2021-03-24 20:39")]
    internal static async ValueTask<(ApiStatus apiStatus, string clientType, string clientVer)> GetClientInfo(
        Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.GetVersion },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, "unknown", null);
        string verStr = ret["data"]?["version"]?.ToString() ?? ret["data"]?["app_version"]?.ToString() ?? string.Empty;

        return (apiStatus, ret["data"]?["app_name"]?.ToString() ?? "unknown", verStr);
    }

    /// <summary>
    /// 获取好友列表
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">服务器连接标识</param>
    /// <returns>好友信息列表</returns>
    [Reviewed("nidbCN", "2021-03-24 20:40")]
    internal static async ValueTask<(ApiStatus apiStatus, List<FriendInfo> friendList)> GetFriendList(
        Guid serviceId,
        Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.GetFriendList },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        //处理返回的好友信息
        List<FriendInfo> friendInfos = ret["data"]?.ToObject<List<FriendInfo>>() ?? new List<FriendInfo>();
        //检查机器人管理员权限
        friendInfos.ForEach(t =>
        {
            t.IsSuperUser = t.UserId is not (0 or -1)
                            && ServiceRecord.IsSuperUser(serviceId, t.UserId);
        });
        return (apiStatus, friendInfos);
    }

    /// <summary>
    /// 获取群组列表
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="useCache">使用缓存</param>
    /// <returns>群组信息列表</returns>
    [Reviewed("nidbCN", "2021-03-24 20:44")]
    internal static async ValueTask<(ApiStatus apiStatus, List<GroupInfo> groupList)> GetGroupList(
        Guid connection,
        bool useCache)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupList,
                                                        ApiParams      = new { no_cache = !useCache }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        //处理返回群组列表
        List<GroupInfo> groupList = ret["data"]?.ToObject<List<GroupInfo>>() ?? new List<GroupInfo>();

        return (apiStatus, groupList);
    }

    /// <summary>
    /// 获取群成员列表
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="useCache">使用缓存</param>
    [Reviewed("nidbCN", "2021-03-24 20:49")]
    internal static async ValueTask<(ApiStatus apiStatus, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList(
        Guid serviceId,
        Guid connection,
        long groupId,
        bool useCache)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupMemberList,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            no_cache = !useCache
                                                        }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        //处理返回群成员列表
        List<GroupMemberInfo> memberList =
            ret["data"]?.ToObject<List<GroupMemberInfo>>() ?? new List<GroupMemberInfo>();
        //检查机器人管理员权限
        memberList.ForEach(t =>
        {
            t.IsSuperUser = t.UserId is not (0 or -1)
                            && ServiceRecord.IsSuperUser(serviceId, t.UserId);
        });

        return (apiStatus, memberList);
    }

    /// <summary>
    /// 获取群信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="useCache">是否使用缓存</param>
    [Reviewed("nidbCN", "2021-03-24 20:55")]
    internal static async ValueTask<(ApiStatus apiStatus, GroupInfo groupInfo)> GetGroupInfo(
        Guid connection,
        long groupId,
        bool useCache)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupInfo,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            no_cache = !useCache
                                                        }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, new GroupInfo());
        return (apiStatus, ret["data"]?.ToObject<GroupInfo>() ?? new GroupInfo());
    }

    /// <summary>
    /// 获取群成员信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="serviceId">服务ID</param>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户ID</param>
    /// <param name="useCache">是否使用缓存</param>
    internal static async ValueTask<(ApiStatus apiStatus, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
        Guid serviceId,
        Guid connection,
        long groupId,
        long userId,
        bool useCache)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupMemberInfo,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            user_id  = userId,
                                                            no_cache = !useCache
                                                        }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, new GroupMemberInfo());
        GroupMemberInfo memberInfo = ret["data"]?.ToObject<GroupMemberInfo>() ?? new GroupMemberInfo();
        //检查服务管理员权限
        memberInfo.IsSuperUser =
            memberInfo.UserId is not (0 or -1) && ServiceRecord.IsSuperUser(serviceId, memberInfo.UserId);
        return (apiStatus, memberInfo);
    }

    /// <summary>
    /// 获取用户信息
    /// 可以为好友或陌生人
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="userId">用户ID</param>
    /// <param name="useCache">使用缓存</param>
    internal static async ValueTask<(ApiStatus apiStatus, UserInfo userInfo, string qid)> GetUserInfo(
        Guid serviceId,
        Guid connection,
        long userId,
        bool useCache)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetStrangerInfo,
                                                        ApiParams = new
                                                        {
                                                            user_id  = userId,
                                                            no_cache = !useCache
                                                        }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, new UserInfo(), string.Empty);
        UserInfo info = ret["data"]?.ToObject<UserInfo>() ?? new UserInfo();
        //检查服务管理员权限
        info.IsSuperUser = info.UserId is not (0 or -1) && ServiceRecord.IsSuperUser(serviceId, info.UserId);

        return (apiStatus, info, ret["data"]?["qid"]?.ToString());
    }

    /// <summary>
    /// 检查是否可以发送图片
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    internal static async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendImage(Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.CanSendImage },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, false);
        return (apiStatus, Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
    }

    /// <summary>
    /// 检查是否可以发送语音
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    internal static async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendRecord(Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.CanSendRecord },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, false);
        return (apiStatus, Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
    }

    /// <summary>
    /// 获取客户端状态
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    internal static async ValueTask<(ApiStatus apiStatus, bool online, bool good, JObject statData)> GetStatus(
        Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest { ApiRequestType = ApiRequestType.GetStatus },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, false, false, null);
        return (apiStatus, Convert.ToBoolean(ret["data"]?["online"]?.ToString() ?? "false"),
            Convert.ToBoolean(ret["data"]?["good"]?.ToString() ?? "false"),
            JObject.FromObject((ret["data"]?["stat"] ?? ret["data"]) ?? new JObject()));
    }

#region GoCQ API

    /// <summary>
    /// 获取图片信息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="cacheFileName">缓存文件名</param>
    internal static async ValueTask<(ApiStatus apiStatus, int size, string fileName, string url)> GetImage(
        Guid   connection,
        string cacheFileName)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetImage,
                                                        ApiParams      = new { file = cacheFileName }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, -1, null, null);
        return (apiStatus, Convert.ToInt32(ret["data"]?["size"] ?? 1), ret["data"]?["filename"]?.ToString(),
            ret["data"]?["url"]?.ToString());
    }

    /// <summary>
    /// 发送合并转发(群)
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="msgList">消息段数组</param>
    /// <param name="timeout">超时覆盖</param>
    internal static async ValueTask<(ApiStatus apiStatus, int messageId, string forwardId)> SendGroupForwardMsg(
        Guid                    connection,
        long                    groupId,
        IEnumerable<CustomNode> msgList,
        TimeSpan?               timeout = null)
    {
        if (msgList == null)
            throw new NullReferenceException("msgList is null or empty");
        //将消息段转换为数组
        CustomNode[] customNodes = msgList as CustomNode[] ?? msgList.ToArray();


        //发送消息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SendGroupForwardMsg,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            messages = customNodes.Select(node => new
                                                            {
                                                                type = "node",
                                                                data = node
                                                            }).ToList()
                                                        }
                                                    },
                                                    connection,
                                                    timeout);
        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, -1, string.Empty);
        int    msgCode = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode) ? messageCode : -1;
        string fwId    = ret["data"]?["forward_id"]?.ToString() ?? string.Empty;
        Log.Debug("Sora", $"Get send_group_forward_msg response [{msgCode}]{nameof(apiStatus)}={apiStatus.RetCode}");
        return (apiStatus, msgCode, fwId);
    }

    /// <summary>
    /// 发送合并转发(群)
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="userId">用户ID</param>
    /// <param name="msgList">消息段数组</param>
    /// <param name="timeout">超时覆盖</param>
    internal static async ValueTask<(ApiStatus apiStatus, int messageId)> SendPrivateForwardMsg(
        Guid                    connection,
        long                    userId,
        IEnumerable<CustomNode> msgList,
        TimeSpan?               timeout = null)
    {
        if (msgList == null)
            throw new NullReferenceException("msgList is null or empty");
        //将消息段转换为数组
        CustomNode[] customNodes = msgList as CustomNode[] ?? msgList.ToArray();


        //发送消息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SendPrivateForwardMsg,
                                                        ApiParams = new
                                                        {
                                                            user_id = userId,
                                                            messages = customNodes.Select(node => new
                                                            {
                                                                type = "node",
                                                                data = node
                                                            }).ToList()
                                                        }
                                                    },
                                                    connection,
                                                    timeout);
        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, -1);
        int msgCode = int.TryParse(ret["data"]?["message_id"]?.ToString(), out int messageCode) ? messageCode : -1;
        Log.Debug("Sora", $"Get send_private_forward_msg response [{msgCode}]{nameof(apiStatus)}={apiStatus.RetCode}");
        return (apiStatus, msgCode);
    }

    /// <summary>
    /// 获取消息
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="msgId">消息ID</param>
    internal static async
        ValueTask<(ApiStatus apiStatus, MessageContext message, User sender, Group sourceGroup, int realId, bool
            isGroupMsg)> GetMessage(Guid serviceId, Guid connection, int msgId)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetMessage,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null, null, null, 0, false);
        //处理消息段
        List<OnebotSegment> rawMessage = ret["data"]?["message"]?.ToObject<List<OnebotSegment>>();
        return (apiStatus,
            message: new MessageContext(connection,
                                        msgId,
                                        ret["data"]?["raw_message"]?.ToString(),
                                        (rawMessage ?? new List<OnebotSegment>()).ToMessageBody(),
                                        Convert.ToInt64(ret["data"]?["time"] ?? -1),
                                        0,
                                        Convert.ToBoolean(ret["data"]?["group"] ?? false)
                                            ? Convert.ToInt32(ret["data"]?["message_seq"] ?? 0)
                                            : null),
            sender: new User(serviceId, connection, Convert.ToInt64(ret["data"]?["sender"]?["user_id"] ?? -1)),
            //判断响应数据中是否有群组信息
            sourceGroup: Convert.ToBoolean(ret["data"]?["group"] ?? false)
                ? new Group(connection, Convert.ToInt64(ret["data"]?["group_id"] ?? 0))
                : null, realId: Convert.ToInt32(ret["data"]?["real_id"] ?? 0),
            isGroupMsg: Convert.ToBoolean(ret["data"]?["message_type"]?.ToString().Equals("group") ?? false));
    }

    /// <summary>
    /// 获取中文分词
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="text">内容</param>
    /// <returns>词组列表</returns>
    internal static async ValueTask<(ApiStatus apiStatus, List<string> slicesList)> GetWordSlices(
        Guid   connection,
        string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new NullReferenceException(nameof(text));

        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetWordSlices,
                                                        ApiParams      = new { content = text }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        return (apiStatus, ret["data"]?["slices"]?.ToObject<List<string>>());
    }

    /// <summary>
    /// 获取合并转发消息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="msgId">合并转发 ID</param>
    /// <returns>ApiResponseCollection</returns>
    internal static async ValueTask<(ApiStatus apiStatus, List<Node> nodeArray)> GetForwardMessage(
        Guid   connection,
        string msgId)
    {
        if (string.IsNullOrEmpty(msgId))
            throw new NullReferenceException(nameof(msgId));

        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetForwardMessage,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, null);
        //转换消息类型
        List<Node> messageList = ret?["data"]?["messages"]?.ToObject<List<Node>>() ?? new List<Node>();
        messageList.ForEach(node => node.MessageBody = node.MessageList.ToMessageBody());
        return (apiStatus, messageList);
    }

    /// <summary>
    /// 获取群系统消息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <returns>消息列表</returns>
    internal static async
        ValueTask<(ApiStatus apiStatus, List<GroupRequestInfo> joinList, List<GroupRequestInfo> invitedList)>
        GetGroupSystemMsg(Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupSystemMsg
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, new List<GroupRequestInfo>(), new List<GroupRequestInfo>());
        //解析消息
        List<GroupRequestInfo> joinList = ret?["data"]?["join_requests"]?.ToObject<List<GroupRequestInfo>>()
                                          ?? new List<GroupRequestInfo>();
        List<GroupRequestInfo> invitedList = ret?["data"]?["invited_requests"]?.ToObject<List<GroupRequestInfo>>()
                                             ?? new List<GroupRequestInfo>();
        return (apiStatus, joinList, invitedList);
    }

    /// <summary>
    /// 获取群文件系统信息
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="connection">连接标识</param>
    /// <returns>文件系统信息</returns>
    internal static async ValueTask<(ApiStatus apiStatus, GroupFileSysInfo fileSysInfo)> GetGroupFileSysInfo(
        long groupId,
        Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupFileSystemInfo,
                                                        ApiParams      = new { group_id = groupId }
                                                    },
                                                    connection);

        return apiStatus.RetCode != ApiStatusType.Ok
            ? (apiStatus, new GroupFileSysInfo())
            //解析消息
            : (apiStatus, ret?["data"]?.ToObject<GroupFileSysInfo>() ?? new GroupFileSysInfo());
    }

    /// <summary>
    /// 获取群根目录文件列表
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="connection">连接标识</param>
    /// <returns>文件列表/文件夹列表</returns>
    internal static async
        ValueTask<(ApiStatus apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
        GetGroupRootFiles(long groupId, Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupRootFiles,
                                                        ApiParams      = new { group_id = groupId }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
        return (apiStatus, ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>() ?? new List<GroupFileInfo>(),
            ret?["data"]?["folders"]?.ToObject<List<GroupFolderInfo>>() ?? new List<GroupFolderInfo>());
    }

    /// <summary>
    /// 获取群子目录文件列表
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="folderId">文件夹ID</param>
    /// <param name="connection">连接标识</param>
    /// <returns>文件列表/文件夹列表</returns>
    internal static async
        ValueTask<(ApiStatus apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
        GetGroupFilesByFolder(long groupId, string folderId, Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupFilesByFolder,
                                                        ApiParams = new
                                                        {
                                                            group_id  = groupId,
                                                            folder_id = folderId
                                                        }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
        return (apiStatus, ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>() ?? new List<GroupFileInfo>(),
            ret?["data"]?["folders"]?.ToObject<List<GroupFolderInfo>>() ?? new List<GroupFolderInfo>());
    }

    /// <summary>
    /// 获取群文件资源链接
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="busId">文件类型</param>
    /// <param name="connection">连接标识</param>
    /// <returns>资源链接</returns>
    internal static async ValueTask<(ApiStatus apiStatus, string fileUrl)> GetGroupFileUrl(
        long   groupId,
        string fileId,
        int    busId,
        Guid   connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupFileUrl,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            file_id  = fileId,
                                                            busid    = busId
                                                        }
                                                    },
                                                    connection);

        return apiStatus.RetCode != ApiStatusType.Ok
            ? (apiStatus, null)
            : (apiStatus, ret?["data"]?["url"]?.ToString());
    }

    /// <summary>
    /// 获取群@全体成员剩余次数
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="connection">连接标识</param>
    /// <returns>配额信息</returns>
    internal static async ValueTask<(ApiStatus apiStatus, bool canAt, short groupRemain, short botRemain)>
        GetGroupAtAllRemain(long groupId, Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupAtAllRemain,
                                                        ApiParams      = new { group_id = groupId }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, false, -1, -1);
        return (apiStatus, Convert.ToBoolean(ret?["data"]?["can_at_all"]),
            Convert.ToInt16(ret?["data"]?["remain_at_all_count_for_group"]),
            Convert.ToInt16(ret?["data"]?["remain_at_all_count_for_uin"]));
    }

    /// <summary>
    /// 图片 OCR
    /// </summary>
    /// <param name="imgId">图片ID</param>
    /// <param name="connection">连接标识</param>
    /// <returns>文字识别信息</returns>
    internal static async ValueTask<(ApiStatus apiStatus, List<TextDetection> texts, string lang)> OcrImage(
        string imgId,
        Guid   connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.Ocr,
                                                        ApiParams      = new { image = imgId }
                                                    },
                                                    connection);


        if (apiStatus.RetCode != ApiStatusType.Ok)
            return (apiStatus, new List<TextDetection>(), string.Empty);
        return (apiStatus, ret?["data"]?["texts"]?.ToObject<List<TextDetection>>(),
            ret?["data"]?["language"]?.ToString());
    }

    /// <summary>
    /// <para>下载文件到缓存目录</para>
    /// <para>注意：此API的调用超时时间是独立于其他API的</para>
    /// </summary>
    /// <param name="url">链接地址</param>
    /// <param name="threadCount">下载线程数</param>
    /// <param name="connection">连接标识</param>
    /// <param name="customHeader">自定义请求头</param>
    /// <param name="timeout">超时(ms)</param>
    /// <returns>文件绝对路径</returns>
    internal static async ValueTask<(ApiStatus apiStatus, string filePath)> DownloadFile(
        string                     url,
        int                        threadCount,
        Guid                       connection,
        Dictionary<string, string> customHeader = null,
        int                        timeout      = 10000)
    {
        //处理自定义请求头
        List<string> customHeaderStr = new();
        if (customHeader != null)
            customHeaderStr.AddRange(customHeader.Select(header => $"{header.Key}={header.Value}"));


        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DownloadFile,
                                                        ApiParams = new
                                                        {
                                                            url,
                                                            thread_count = threadCount,
                                                            headers      = customHeaderStr
                                                        }
                                                    },
                                                    connection,
                                                    TimeSpan.FromMilliseconds(timeout));


        return apiStatus.RetCode != ApiStatusType.Ok
            ? (apiStatus, string.Empty)
            : (apiStatus, ret?["data"]?["file"]?.ToString());
    }

    /// <summary>
    /// 获取群消息历史记录
    /// </summary>
    /// <param name="msgSeq">消息序号*</param>
    /// <param name="groupId">群号</param>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">连接标识</param>
    /// <returns>消息</returns>
    internal static async ValueTask<(ApiStatus apiStatus, List<GroupMessageEventArgs> msgList)> GetGroupMessageHistory(
        long? msgSeq,
        long  groupId,
        Guid  serviceId,
        Guid  connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetGroupMsgHistory,
                                                        ApiParams = msgSeq == null
                                                            ? new
                                                            {
                                                                group_id = groupId
                                                            }
                                                            : new
                                                            {
                                                                message_seq = msgSeq,
                                                                group_id    = groupId
                                                            }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        //处理消息段
        return (apiStatus, ret["data"]?["messages"]?
                           .ToObject<List<OnebotGroupMsgEventArgs>>()?
                           .Select(messageArg => new GroupMessageEventArgs(serviceId,
                                                                           connection,
                                                                           "group",
                                                                           messageArg))
                           .ToList());
    }

    /// <summary>
    /// 获取当前账号在线客户端列表
    /// </summary>
    /// <param name="useCache">是否使用缓存</param>
    /// <param name="connection">连接标识</param>
    /// <returns>在线客户端信息</returns>
    internal static async ValueTask<(ApiStatus apiStatus, List<ClientInfo> clients)> GetOnlineClients(
        bool useCache,
        Guid connection)
    {
        //发送信息
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetOnlineClients,
                                                        ApiParams      = new { no_cache = !useCache }
                                                    },
                                                    connection);

        if (apiStatus.RetCode != ApiStatusType.Ok || ret?["data"] == null)
            return (apiStatus, null);
        //处理客户端信息
        return (apiStatus, ret["data"]?["clients"]?.ToObject<List<ClientInfo>>() ?? new List<ClientInfo>());
    }

    /// <summary>
    /// 获取群精华消息列表
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    internal static async ValueTask<(ApiStatus apiStatus, List<EssenceInfo> essenceInfos)> GetEssenceMsgList(
        Guid serviceId,
        Guid connection,
        long groupId)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetEssenceMsgList,
                                                        ApiParams      = new { group_id = groupId }
                                                    },
                                                    connection);

        return (apiStatus,
            (ret?["data"] ?? new JArray()).Select(element => new EssenceInfo(element, serviceId, connection)).ToList());
    }

    /// <summary>
    /// 检查链接安全性
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="url">需要检查的链接</param>
    internal static async ValueTask<(ApiStatus apiStatus, SecurityLevelType securityLevel)> CheckUrlSafely(
        Guid   connection,
        string url)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.CheckUrlSafely,
                                                        ApiParams      = new { url }
                                                    },
                                                    connection);

        return (apiStatus, (SecurityLevelType)Convert.ToInt32(ret?["data"]?["level"] ?? 1));
    }

    /// <summary>
    /// <para>获取企点账号信息</para>
    /// <para>该API只有企点协议可用</para>
    /// </summary>
    /// <param name="connection">链接标识</param>
    internal static async ValueTask<(ApiStatus apiStatus, QidianAccountInfo qidianAccountInfo)> GetQidianAccountInfo(
        Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetQidianAccountInfo
                                                    },
                                                    connection);

        return (apiStatus, ret?["data"]?.ToObject<QidianAccountInfo>() ?? new QidianAccountInfo());
    }

    /// <summary>
    /// 获取在线机型
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="model">型号</param>
    internal static async ValueTask<(ApiStatus apiStatus, List<ModelInfo> models)> GetModelShow(
        Guid   connection,
        string model)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetModelShow,
                                                        ApiParams      = new { model }
                                                    },
                                                    connection);

        return (apiStatus, ret?["data"]?["variants"]?.ToObject<List<ModelInfo>>() ?? new List<ModelInfo>());
    }

    /// <summary>
    /// 获取单向好友列表
    /// </summary>
    /// <param name="connection">连接标识</param>
    internal static async ValueTask<(ApiStatus apiStatus, List<UnidirectionalFriendInfo> unidirectionalFriendInfos)>
        GetUnidirectionalFriendList(Guid connection)
    {
        (ApiStatus apiStatus, JObject ret) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.GetUnidirectionalFriendList
                                                    },
                                                    connection);

        return (apiStatus,
            ret?["data"]?.ToObject<List<UnidirectionalFriendInfo>>() ?? new List<UnidirectionalFriendInfo>());
    }

#endregion

#endregion

#region 无响应数据的API请求

    /// <summary>
    /// 撤回消息
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="msgId">消息id</param>
    internal static async ValueTask<ApiStatus> RecallMsg(Guid connection, int msgId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.RecallMsg,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 处理加好友请求
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="flag">请求flag</param>
    /// <param name="approve">是否同意</param>
    /// <param name="remark">好友备注</param>
    internal static async ValueTask<ApiStatus> SetFriendAddRequest(Guid   connection,
                                                                   string flag,
                                                                   bool   approve,
                                                                   string remark = null)
    {
        if (string.IsNullOrEmpty(flag))
            throw new NullReferenceException(nameof(flag));

        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetFriendAddRequest,
                                                        ApiParams = new
                                                        {
                                                            flag,
                                                            approve,
                                                            remark
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 处理加群请求/邀请
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="flag">请求flag</param>
    /// <param name="requestType">请求类型</param>
    /// <param name="approve">是否同意</param>
    /// <param name="reason">好友备注</param>
    internal static async ValueTask<ApiStatus> SetGroupAddRequest(Guid             connection,
                                                                  string           flag,
                                                                  GroupRequestType requestType,
                                                                  bool             approve,
                                                                  string           reason = null)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupAddRequest,
                                                        ApiParams = new SetGroupAddRequestParams
                                                        {
                                                            Flag             = flag,
                                                            GroupRequestType = requestType,
                                                            Approve          = approve,
                                                            Reason           = reason
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置群名片
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户id</param>
    /// <param name="card">新名片</param>
    internal static async ValueTask<ApiStatus> SetGroupCard(Guid connection, long groupId, long userId, string card)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupCard,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            user_id  = userId,
                                                            card
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置群组专属头衔
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户id</param>
    /// <param name="title">头衔</param>
    internal static async ValueTask<ApiStatus> SetGroupSpecialTitle(Guid   connection,
                                                                    long   groupId,
                                                                    long   userId,
                                                                    string title)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupSpecialTitle,
                                                        ApiParams = new
                                                        {
                                                            group_id      = groupId,
                                                            user_id       = userId,
                                                            special_title = title,
                                                            duration      = -1
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 群组踢人
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户id</param>
    /// <param name="rejectRequest">拒绝此人的加群请求</param>
    internal static async ValueTask<ApiStatus> KickGroupMember(Guid connection,
                                                               long groupId,
                                                               long userId,
                                                               bool rejectRequest)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupKick,
                                                        ApiParams = new
                                                        {
                                                            group_id           = groupId,
                                                            user_id            = userId,
                                                            reject_add_request = rejectRequest
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 群组单人禁言
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="userId">用户id</param>
    /// <param name="duration">禁言时长(s)</param>
    internal static async ValueTask<ApiStatus> SetGroupBan(Guid connection, long groupId, long userId, long duration)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupBan,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            user_id  = userId,
                                                            duration
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 群组全员禁言
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="enable">是否禁言</param>
    internal static async ValueTask<ApiStatus> SetGroupWholeBan(Guid connection, long groupId, bool enable)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupWholeBan,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            enable
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 群组匿名用户禁言
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="anonymous">匿名用户对象</param>
    /// <param name="duration">禁言时长, 单位秒</param>
    internal static async ValueTask<ApiStatus> SetAnonymousBan(Guid      connection,
                                                               long      groupId,
                                                               Anonymous anonymous,
                                                               long      duration)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            anonymous,
                                                            duration
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 群组匿名用户禁言
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="anonymousFlag">匿名用户flag</param>
    /// <param name="duration">禁言时长, 单位秒</param>
    internal static async ValueTask<ApiStatus> SetAnonymousBan(Guid   connection,
                                                               long   groupId,
                                                               string anonymousFlag,
                                                               long   duration)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            flag     = anonymousFlag,
                                                            duration
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置群管理员
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="userId">成员id</param>
    /// <param name="groupId">群号</param>
    /// <param name="enable">设置或取消</param>
    internal static async ValueTask<ApiStatus> SetGroupAdmin(Guid connection, long userId, long groupId, bool enable)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupAdmin,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            user_id  = userId,
                                                            enable
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 退出/解散群
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="dismiss">是否解散</param>
    internal static async ValueTask<ApiStatus> SetGroupLeave(Guid connection, long groupId, bool dismiss)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupLeave,
                                                        ApiParams = new
                                                        {
                                                            group_id   = groupId,
                                                            is_dismiss = dismiss
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 重启客户端
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="delay">延迟(ms)</param>
    internal static async ValueTask<ApiStatus> Restart(Guid connection, int delay)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.Restart,
                                                        ApiParams      = new { delay }
                                                    },
                                                    connection);

        return apiStatus;
    }

#region GoCQ API

    /// <summary>
    /// 设置群名
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="name">新群名</param>
    internal static async ValueTask<ApiStatus> SetGroupName(Guid connection, long groupId, string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new NullReferenceException(nameof(name));

        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupName,
                                                        ApiParams = new
                                                        {
                                                            group_id   = groupId,
                                                            group_name = name
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置群头像
    /// </summary>
    /// <param name="connection">服务器连接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="file">图片文件</param>
    /// <param name="useCache">是否使用缓存</param>
    internal static async ValueTask<ApiStatus> SetGroupPortrait(Guid   connection,
                                                                long   groupId,
                                                                string file,
                                                                bool   useCache)
    {
        if (string.IsNullOrEmpty(file))
            throw new NullReferenceException(nameof(file));

        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetGroupPortrait,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            file,
                                                            cache = useCache ? 1 : 0
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 重载事件过滤器
    /// </summary>
    /// <param name="connection">连接标识</param>
    internal static async ValueTask<ApiStatus> ReloadEventFilter(Guid connection)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.ReloadEventFilter
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 上传群文件
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="localFilePath">本地文件路径</param>
    /// <param name="fileName">上传文件名</param>
    /// <param name="folderId">父目录ID 为<see langword="null"/>时则上传到根目录</param>
    /// <param name="timeout">超时</param>
    internal static async ValueTask<ApiStatus> UploadGroupFile(Guid   connection,
                                                               long   groupId,
                                                               string localFilePath,
                                                               string fileName,
                                                               string folderId = null,
                                                               int    timeout  = 10000)
    {
        if (localFilePath is null || fileName is null) throw new NullReferenceException("para is null");

        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.UploadGroupFile,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            file     = localFilePath,
                                                            name     = fileName,
                                                            folder   = folderId ?? string.Empty
                                                        }
                                                    },
                                                    connection,
                                                    TimeSpan.FromMilliseconds(timeout));

        return apiStatus;
    }

    /// <summary>
    /// 上传私聊文件
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="userId">用户ID</param>
    /// <param name="localFilePath">本地文件路径</param>
    /// <param name="fileName">上传文件名</param>
    /// <param name="timeout">超时</param>
    internal static async ValueTask<ApiStatus> UploadPrivateFile(Guid   connection,
                                                                 long   userId,
                                                                 string localFilePath,
                                                                 string fileName,
                                                                 int    timeout = 10000)
    {
        if (localFilePath is null || fileName is null) throw new NullReferenceException("para is null");

        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.UploadPrivateFile,
                                                        ApiParams = new
                                                        {
                                                            user_id = userId,
                                                            file    = localFilePath,
                                                            name    = fileName
                                                        }
                                                    },
                                                    connection,
                                                    TimeSpan.FromMilliseconds(timeout));

        return apiStatus;
    }

    /// <summary>
    /// 设置精华消息
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="msgId">消息ID</param>
    internal static async ValueTask<ApiStatus> SetEssenceMsg(Guid connection, long msgId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetEssenceMsg,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 删除精华消息
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="msgId">消息ID</param>
    internal static async ValueTask<ApiStatus> DelEssenceMsg(Guid connection, long msgId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteEssenceMsg,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 发送群公告
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="content">公告内容</param>
    /// <param name="image">图片</param>
    internal static async ValueTask<ApiStatus> SendGroupNotice(Guid   connection,
                                                               long   groupId,
                                                               string content,
                                                               string image)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SendGroupNotice,
                                                        ApiParams = string.IsNullOrEmpty(image)
                                                            ? new
                                                            {
                                                                group_id = groupId,
                                                                content
                                                            }
                                                            : new
                                                            {
                                                                group_id = groupId,
                                                                content,
                                                                image
                                                            }
                                                    },
                                                    connection);

        return apiStatus;
    }

    internal static async ValueTask<ApiStatus> DelGroupNotice(Guid   connection,
                                                              long   groupId,
                                                              string noticeId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteGroupNotice,
                                                        ApiParams = new
                                                        {
                                                            group_id  = groupId,
                                                            notice_id = noticeId
                                                        }
                                                    },
                                                    connection);
        return apiStatus;
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="userId">用户id</param>
    internal static async ValueTask<ApiStatus> DeleteFriend(Guid connection, long userId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteFriend,
                                                        ApiParams      = new { id = userId }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置在线机型
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="model">机型名</param>
    /// <param name="showModel">展示名</param>
    internal static async ValueTask<ApiStatus> SetModelShow(Guid connection, string model, string showModel)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetModelShow,
                                                        ApiParams = new
                                                        {
                                                            model,
                                                            model_show = showModel
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 创建群文件夹
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="name">文件夹名</param>
    /// <param name="folderId">文件夹ID</param>
    internal static async ValueTask<ApiStatus> CreateGroupFileFolder(Guid   connection,
                                                                     long   groupId,
                                                                     string name,
                                                                     string folderId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.CreateGroupFileFolder,
                                                        ApiParams = string.IsNullOrEmpty(folderId)
                                                            ? new
                                                            {
                                                                group_id = groupId,
                                                                name
                                                            }
                                                            : new
                                                            {
                                                                group_id = groupId,
                                                                name,
                                                                folder_id = folderId
                                                            }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 删除群文件文件夹
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="folderId">文件夹ID</param>
    internal static async ValueTask<ApiStatus> DeleteGroupFolder(Guid connection, long groupId, string folderId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteGroupFolder,
                                                        ApiParams = new
                                                        {
                                                            group_id  = groupId,
                                                            folder_id = folderId
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 删除群文件文件夹
    /// </summary>
    /// <param name="connection">链接标识</param>
    /// <param name="groupId">群号</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="busId">文件类型</param>
    internal static async ValueTask<ApiStatus> DeleteGroupFile(Guid connection, long groupId, string fileId, int busId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteGroupFile,
                                                        ApiParams = new
                                                        {
                                                            group_id = groupId,
                                                            file_id  = fileId,
                                                            bus_id   = busId
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 标记消息已读
    /// </summary>
    /// <param name="connection">连接标识</param>
    /// <param name="msgId">消息ID</param>
    internal static async ValueTask<ApiStatus> MarkMessageRead(Guid connection, int msgId)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.MarkMsgAsRead,
                                                        ApiParams      = new { message_id = msgId }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 标记消息已读
    /// </summary>
    /// <param name="connection">连接标识</param>
    /// <param name="msgId">消息ID</param>
    internal static void InternalMarkMessageRead(Guid connection, int msgId)
    {
        Task.Run(async () =>
        {
            Log.Debug("Sora", "Auto mark message read request send");
            (ApiStatus apiStatus, _) =
                await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                        {
                                                            ApiRequestType = ApiRequestType.MarkMsgAsRead,
                                                            ApiParams      = new { message_id = msgId }
                                                        },
                                                        connection);
            Log.Debug("Sora",
                      apiStatus.RetCode == ApiStatusType.Ok
                          ? "Auto mark message read success"
                          : "Auto mark message read failed");
        });
    }

    /// <summary>
    /// 删除单向好友
    /// </summary>
    /// <param name="connection">连接标识</param>
    /// <param name="uid">uid</param>
    internal static async ValueTask<ApiStatus> DeleteUnidirectionalFriend(Guid connection, long uid)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.DeleteUnidirectionalFriend,
                                                        ApiParams      = new { user_id = uid }
                                                    },
                                                    connection);

        return apiStatus;
    }

    /// <summary>
    /// 设置 QQ 个人资料
    /// </summary>
    /// <param name="connection">连接标识</param>
    /// <param name="profile">个人资料</param>
    internal static async ValueTask<ApiStatus> SetQQProfile(Guid connection, ProfileDetail profile)
    {
        (ApiStatus apiStatus, _) =
            await ReactiveApiManager.SendApiRequest(new ApiRequest
                                                    {
                                                        ApiRequestType = ApiRequestType.SetQQProfile,
                                                        ApiParams = new
                                                        {
                                                            nickname      = profile.Nick,
                                                            company       = profile.Company,
                                                            email         = profile.Email,
                                                            college       = profile.College,
                                                            personal_note = profile.PersonalNote
                                                        }
                                                    },
                                                    connection);

        return apiStatus;
    }

#endregion

#endregion
}