using Newtonsoft.Json.Linq;
using Sora.Entities;
using Sora.Entities.MessageSegment.Segment;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Net;
using Sora.OnebotModel.ApiParams;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sora.Attributes;
using Sora.Converter;
using Sora.Enumeration;
using YukariToolBox.FormatLog;

namespace Sora.OnebotInterface
{
    internal static class ApiInterface
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
            Guid connection, long target, MessageBody messages, long? groupId, TimeSpan? timeout)
        {
            Log.Debug("Sora", "Sending send_msg(Private) request");
            if (messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Private,
                    UserId      = target,
                    //转换消息段列表
                    Message = messages.Where(msg => msg != null && msg.MessageType != SegmentType.Ignore)
                                      .Select(msg => msg.ToOnebotMessage())
                                      .ToList(),
                    GroupId = groupId
                }
            }, connection, timeout);
            Log.Debug("Sora", $"Get send_msg(Private) response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK) return (apiStatus, -1);
            var msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out var messageCode)
                ? messageCode
                : -1;
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
            Guid connection, long target, MessageBody messages, TimeSpan? timeout)
        {
            Log.Debug("Sora", "Sending send_msg(Group) request");
            if (messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Group,
                    GroupId     = target,
                    //转换消息段列表
                    Message = messages.Where(msg => msg != null && msg.MessageType != SegmentType.Ignore)
                                      .Select(msg => msg.ToOnebotMessage())
                                      .ToList(),
                }
            }, connection, timeout);
            Log.Debug("Sora", $"Get send_msg(Group) response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK) return (apiStatus, -1);
            var msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out var messageCode)
                ? messageCode
                : -1;
            Log.Debug("Sora", $"msg send -> group[{target}]");
            return (apiStatus, msgCode);
        }

        /// <summary>
        /// 获取登陆账号信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>ApiResponseCollection</returns>
        [Reviewed("nidbCN", "2021-03-24 20:38")]
        internal static async ValueTask<(ApiStatus apiStatus, long userId, string nick)> GetLoginInfo(Guid connection)
        {
            Log.Debug("Sora", "Sending get_login_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetLoginInfo
            }, connection);
            Log.Debug("Sora", $"Get get_login_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK) return (apiStatus, -1, null);
            return
            (
                apiStatus,
                userId: long.TryParse(ret?["data"]?["user_id"]?.ToString(), out var userId) ? userId : -1,
                nick: ret?["data"]?["nickname"]?.ToString() ?? string.Empty
            );
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        [Reviewed("nidbCN", "2021-03-24 20:39")]
        internal static async ValueTask<(ApiStatus apiStatus, string clientType, string clientVer)> GetClientInfo(
            Guid connection)
        {
            Log.Debug("Sora", "Sending get_version_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetVersion
            }, connection);
            Log.Debug("Sora", $"Get get_version_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, "unknown", null);
            var verStr = ret["data"]?["version"]?.ToString() ?? ret["data"]?["app_version"]?.ToString() ?? string.Empty;

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
            Guid serviceId, Guid connection)
        {
            Log.Debug("Sora", "Sending get_friend_list request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetFriendList
            }, connection);
            Log.Debug("Sora", $"Get get_friend_list response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
            //处理返回的好友信息
            var friendInfos = ret["data"]?.Select(token => new FriendInfo
            {
                UserId = Convert.ToInt64(token["user_id"] ?? -1),
                Remark = token["remark"]?.ToString()   ?? string.Empty,
                Nick   = token["nickname"]?.ToString() ?? string.Empty,
                Role = Convert.ToInt64(token["user_id"] ?? -1) != -1 && StaticVariable.ServiceInfos[serviceId]
                    .SuperUsers
                    .Contains(Convert.ToInt64(token["user_id"]))
                    ? MemberRoleType.SuperUser
                    : MemberRoleType.Member
            }).ToList();

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
            Guid connection, bool useCache)
        {
            Log.Debug("Sora", "Sending get_group_list request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupList,
                ApiParams = new
                {
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_list response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
            //处理返回群组列表
            var groupList = ret["data"]?.Select(token => token.ToObject<GroupInfo>()).ToList();

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
        internal static async ValueTask<(ApiStatus apiStatus, List<GroupMemberInfo> groupMemberList)>
            GetGroupMemberList(Guid serviceId, Guid connection, long groupId, bool useCache)
        {
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupMemberList,
                ApiParams = new
                {
                    group_id = groupId,
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_member_list response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
            //处理返回群成员列表
            var memberList = ret["data"]?.ToObject<List<GroupMemberInfo>>() ?? new List<GroupMemberInfo>();
            //检查最高级管理员权限
            foreach (var t in memberList.Where(t => StaticVariable.ServiceInfos[serviceId].SuperUsers
                                                                  .Contains(t.UserId)))
                t.Role = MemberRoleType.SuperUser;

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
            Guid connection, long groupId, bool useCache)
        {
            Log.Debug("Sora", "Sending get_group_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupInfo,
                ApiParams = new
                {
                    group_id = groupId,
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, new GroupInfo());
            return (apiStatus,
                    ret["data"]?.ToObject<GroupInfo>() ?? new GroupInfo());
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
            Guid serviceId, Guid connection, long groupId, long userId, bool useCache)
        {
            Log.Debug("Sora", "Sending get_group_member_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupMemberInfo,
                ApiParams = new
                {
                    group_id = groupId,
                    user_id  = userId,
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_member_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null)
                return (apiStatus, new GroupMemberInfo());
            var memberInfo = ret["data"]?.ToObject<GroupMemberInfo>() ?? new GroupMemberInfo();
            if (memberInfo.UserId != 0 && StaticVariable.ServiceInfos[serviceId].SuperUsers
                                                        .Contains(memberInfo.UserId))
                memberInfo.Role = MemberRoleType.SuperUser;
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
            Guid serviceId, Guid connection, long userId, bool useCache)
        {
            Log.Debug("Sora", "Sending get_stranger_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetStrangerInfo,
                ApiParams = new
                {
                    user_id  = userId,
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_stranger_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null)
                return (apiStatus, new UserInfo(), string.Empty);
            //检查服务管理员权限
            var info = ret["data"]?.ToObject<UserInfo>() ?? new UserInfo();
            if (info.UserId is not 0 or -1)
            {
                info.Role = info.UserId != -1 && StaticVariable.ServiceInfos[serviceId].SuperUsers
                                                               .Contains(info.UserId)
                    ? MemberRoleType.SuperUser
                    : MemberRoleType.Member;
            }

            return (apiStatus, info, ret["data"]?["qid"]?.ToString());
        }

        /// <summary>
        /// 检查是否可以发送图片
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendImage(Guid connection)
        {
            Log.Debug("Sora", "Sending can_send_image request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CanSendImage
            }, connection);
            Log.Debug("Sora", $"Get can_send_image response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, false);
            return (apiStatus,
                    Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
        }

        /// <summary>
        /// 检查是否可以发送语音
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendRecord(Guid connection)
        {
            Log.Debug("Sora", "Sending can_send_record request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CanSendRecord
            }, connection);
            Log.Debug("Sora", $"Get can_send_record response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, false);
            return (apiStatus,
                    Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(ApiStatus apiStatus, bool online, bool good, JObject statData)> GetStatus(
            Guid connection)
        {
            Log.Debug("Sora", "Sending get_status request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetStatus
            }, connection);
            Log.Debug("Sora", $"Get get_status response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, false, false, null);
            return (apiStatus,
                    Convert.ToBoolean(ret["data"]?["online"]?.ToString()     ?? "false"),
                    Convert.ToBoolean(ret["data"]?["good"]?.ToString()       ?? "false"),
                    JObject.FromObject((ret["data"]?["stat"] ?? ret["data"]) ?? new JObject()));
        }

        #region Go API

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="cacheFileName">缓存文件名</param>
        internal static async ValueTask<(ApiStatus apiStatus, int size, string fileName, string url)> GetImage(
            Guid connection, string cacheFileName)
        {
            Log.Debug("Sora", "Sending get_image request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetImage,
                ApiParams = new
                {
                    file = cacheFileName
                }
            }, connection);
            Log.Debug("Sora", $"Get get_image response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, -1, null, null);
            return (apiStatus,
                    Convert.ToInt32(ret["data"]?["size"] ?? 1),
                    ret["data"]?["filename"]?.ToString(),
                    ret["data"]?["url"]?.ToString());
        }

        /// <summary>
        /// 获取消息
        /// </summary>
        /// <param name="serviceId">服务ID</param>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async
            ValueTask<(ApiStatus apiStatus, Message message, User sender, Group sourceGroup, int
                realId, bool
                isGroupMsg)> GetMessage(
                Guid serviceId, Guid connection, int msgId)
        {
            Log.Debug("Sora", "Sending get_msg request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetMessage,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null)
                return (apiStatus, null, null, null, 0, false);
            //处理消息段
            var rawMessage = ret["data"]?["message"]?.ToObject<List<OnebotMessageElement>>();
            return (apiStatus,
                    message: new Message(serviceId,
                                         connection,
                                         msgId,
                                         ret["data"]?["raw_message"]?.ToString(),
                                         MessageConverter.Parse(rawMessage    ?? new List<OnebotMessageElement>()),
                                         Convert.ToInt64(ret["data"]?["time"] ?? -1),
                                         0,
                                         Convert.ToBoolean(ret["data"]?["group"]           ?? false)
                                             ? Convert.ToInt32(ret["data"]?["message_seq"] ?? 0)
                                             : null),
                    sender: new User(serviceId, connection,
                                     Convert.ToInt64(ret["data"]?["sender"]?["user_id"] ?? -1)),
                    //判断响应数据中是否有群组信息
                    sourceGroup: Convert.ToBoolean(ret["data"]?["group"] ?? false)
                        ? new Group(serviceId, connection, Convert.ToInt64(ret["data"]?["group_id"] ?? 0))
                        : null,
                    realId: Convert.ToInt32(ret["data"]?["real_id"]                                        ?? 0),
                    isGroupMsg: Convert.ToBoolean(ret["data"]?["message_type"]?.ToString().Equals("group") ?? false));
        }

        /// <summary>
        /// 获取中文分词
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="text">内容</param>
        /// <returns>词组列表</returns>
        internal static async ValueTask<(ApiStatus apiStatus, List<string> slicesList)> GetWordSlices(
            Guid connection, string text)
        {
            if (string.IsNullOrEmpty(text)) throw new NullReferenceException(nameof(text));
            Log.Debug("Sora", "Sending .get_word_slices request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetWordSlices,
                ApiParams = new
                {
                    content = text
                }
            }, connection);
            Log.Debug("Sora", $"Get .get_word_slices response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
            return (apiStatus,
                    ret["data"]?["slices"]?.ToObject<List<string>>());
        }

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">合并转发 ID</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<(ApiStatus apiStatus, NodeArray nodeArray)> GetForwardMessage(
            Guid connection, string msgId)
        {
            if (string.IsNullOrEmpty(msgId)) throw new NullReferenceException(nameof(msgId));
            Log.Debug("Sora", "Sending get_forward_msg request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetForwardMessage,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_forward_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK) return (apiStatus, null);
            //转换消息类型
            var messageList = ret?["data"]?.ToObject<NodeArray>() ?? new NodeArray();
            messageList.ParseNode();
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
            Log.Debug("Sora", "Sending get_group_system_msg request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupSystemMsg
            }, connection);
            Log.Debug("Sora", $"Get get_group_system_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK)
                return (apiStatus, new List<GroupRequestInfo>(), new List<GroupRequestInfo>());
            //解析消息
            var joinList =
                ret?["data"]?["join_requests"]?.ToObject<List<GroupRequestInfo>>() ??
                new List<GroupRequestInfo>();
            var invitedList =
                ret?["data"]?["invited_requests"]?.ToObject<List<GroupRequestInfo>>() ??
                new List<GroupRequestInfo>();
            return (apiStatus, joinList, invitedList);
        }

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文件系统信息</returns>
        internal static async
            ValueTask<(ApiStatus apiStatus, GroupFileSysInfo fileSysInfo)> GetGroupFileSysInfo(
                long groupId, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_file_system_info request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFileSystemInfo,
                ApiParams = new
                {
                    group_id = groupId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_file_system_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus.RetCode != ApiStatusType.OK
                ? (apiStatus, new GroupFileSysInfo())
                : (apiStatus, ret?["data"]?.ToObject<GroupFileSysInfo>() ?? new GroupFileSysInfo());
            //解析消息
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
            Log.Debug("Sora", "Sending get_group_root_files request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupRootFiles,
                ApiParams = new
                {
                    group_id = groupId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_root_files response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK)
                return (apiStatus, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
            return (apiStatus,
                    ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>()     ?? new List<GroupFileInfo>(),
                    ret?["data"]?["folders"]?.ToObject<List<GroupFolderInfo>>() ?? new List<GroupFolderInfo>());
        }

        /// <summary>
        /// 获取群子目录文件列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="folderID">文件夹ID</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文件列表/文件夹列表</returns>
        internal static async
            ValueTask<(ApiStatus apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupFilesByFolder(long groupId, string folderID, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_files_by_folder request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFilesByFolder,
                ApiParams = new
                {
                    group_id  = groupId,
                    folder_id = folderID
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_files_by_folder response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK)
                return (apiStatus, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
            return (apiStatus,
                    ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>()     ?? new List<GroupFileInfo>(),
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
            long groupId, string fileId, int busId, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_file_url request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFileUrl,
                ApiParams = new
                {
                    group_id = groupId,
                    file_id  = fileId,
                    busid    = busId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_file_url response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus.RetCode != ApiStatusType.OK
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
            Log.Debug("Sora", "Sending get_group_at_all_remain request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupAtAllRemain,
                ApiParams = new
                {
                    group_id = groupId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_group_at_all_remain response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK)
                return (apiStatus, false, -1, -1);
            else
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
            string imgId, Guid connection)
        {
            Log.Debug("Sora", "Sending ocr_image request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.Ocr,
                ApiParams = new
                {
                    image = imgId
                }
            }, connection);
            Log.Debug("Sora", $"Get ocr_image response {nameof(apiStatus)}={apiStatus.RetCode}");

            if (apiStatus.RetCode != ApiStatusType.OK)
                return (apiStatus, new List<TextDetection>(), string.Empty);
            else
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
            string url, int threadCount, Guid connection, Dictionary<string, string> customHeader = null,
            int timeout = 10000)
        {
            //处理自定义请求头
            List<string> customHeaderStr = new();
            if (customHeader != null)
            {
                customHeaderStr.AddRange(customHeader.Select(header => $"{header.Key}={header.Value}"));
            }

            Log.Debug("Sora", "Sending download_file request");

            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DownloadFile,
                ApiParams = new
                {
                    url,
                    thread_count = threadCount,
                    headers      = customHeaderStr
                }
            }, connection, TimeSpan.FromMilliseconds(timeout));

            Log.Debug("Sora", $"Get download_file response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus.RetCode != ApiStatusType.OK
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
        internal static async ValueTask<(ApiStatus apiStatus, List<GroupMessageEventArgs> msgList)>
            GetGroupMessageHistory(
                long? msgSeq, long groupId, Guid serviceId, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_msg_history request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
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
            }, connection);
            Log.Debug("Sora", $"Get get_group_msg_history response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
            //处理消息段
            return (apiStatus, ret["data"]?["messages"]?.ToObject<List<OnebotGroupMsgEventArgs>>()
                                                       ?.Select(messageArg =>
                                                                    new GroupMessageEventArgs(serviceId, connection,
                                                                        "group",
                                                                        messageArg)).ToList());
        }

        /// <summary>
        /// 获取当前账号在线客户端列表
        /// </summary>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="connection">连接标识</param>
        /// <returns>在线客户端信息</returns>
        internal static async ValueTask<(ApiStatus apiStatus, List<ClientInfo> clients)> GetOnlineClients(
            bool useCache, Guid connection)
        {
            Log.Debug("Sora", "Sending get_online_clients request");
            //发送信息
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetOnlineClients,
                ApiParams = new
                {
                    no_cache = !useCache
                }
            }, connection);
            Log.Debug("Sora", $"Get get_online_clients response {nameof(apiStatus)}={apiStatus.RetCode}");
            if (apiStatus.RetCode != ApiStatusType.OK || ret?["data"] == null) return (apiStatus, null);
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
            Guid serviceId, Guid connection, long groupId)
        {
            Log.Debug("Sora", "Sending get_essence_msg_list request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetEssenceMsgList,
                ApiParams = new
                {
                    group_id = groupId
                }
            }, connection);
            Log.Debug("Sora", $"Get get_essence_msg_list response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus, (ret?["data"] ?? new JArray())
                               .Select(element => new EssenceInfo(element, serviceId, connection)).ToList());
        }

        /// <summary>
        /// 检查链接安全性
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="url">需要检查的链接</param>
        internal static async ValueTask<(ApiStatus apiStatus, SecurityLevelType securityLevel)> CheckUrlSafely(
            Guid connection, string url)
        {
            Log.Debug("Sora", "Sending check_url_safely request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CheckUrlSafely,
                ApiParams = new
                {
                    url
                }
            }, connection);
            Log.Debug("Sora", $"Get check_url_safely response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus, (SecurityLevelType)Convert.ToInt32(ret?["data"]?["level"] ?? 1));
        }

        /// <summary>
        /// 获取vip信息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="userId">需要检查的链接</param>
        [Obsolete]
        internal static async ValueTask<(ApiStatus apiStatus, VipInfo securityLevel)> GetVipInfo(
            Guid connection, long userId)
        {
            Log.Debug("Sora", "Sending _get_vip_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._GetVipInfo,
                ApiParams = new
                {
                    user_id = userId
                }
            }, connection);
            Log.Debug("Sora", $"Get _get_vip_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus, ret?["data"]?.ToObject<VipInfo>() ?? new VipInfo());
        }

        /// <summary>
        /// <para>获取企点账号信息</para>
        /// <para>该API只有企点协议可用</para>
        /// </summary>
        /// <param name="connection">链接标识</param>
        internal static async ValueTask<(ApiStatus apiStatus, QidianAccountInfo qidianAccountInfo)>
            GetQidianAccountInfo(
                Guid connection)
        {
            Log.Debug("Sora", "Sending qidian_get_account_info request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetQidianAccountInfo
            }, connection);
            Log.Debug("Sora", $"Get qidian_get_account_info response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus, ret?["data"]?.ToObject<QidianAccountInfo>() ?? new QidianAccountInfo());
        }

        /// <summary>
        /// 获取在线机型
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="model">型号</param>
        internal static async ValueTask<(ApiStatus apiStatus, List<Model> models)> GetModelShow(
            Guid connection, string model)
        {
            Log.Debug("Sora", "Sending _get_model_show request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._GetModelShow,
                ApiParams = new
                {
                    model
                }
            }, connection);
            Log.Debug("Sora", $"Get _get_model_show response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus, ret?["data"]?["variants"]?.ToObject<List<Model>>() ?? new List<Model>());
        }

        /// <summary>
        /// 获取单向好友列表
        /// </summary>
        /// <param name="connection">连接标识</param>
        internal static async ValueTask<(ApiStatus apiStatus, List<UnidirectionalFriendInfo> unidirectionalFriendInfos)>
            GetUnidirectionalFriendList(Guid connection)
        {
            Log.Debug("Sora", "Sending get_unidirectional_friend_list request");
            var (apiStatus, ret) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetUnidirectionalFriendList
            }, connection);
            Log.Debug("Sora", $"Get get_unidirectional_friend_list response {nameof(apiStatus)}={apiStatus.RetCode}");
            return (apiStatus,
                    ret?["data"]?.ToObject<List<UnidirectionalFriendInfo>>() ??
                    new List<UnidirectionalFriendInfo>());
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
            Log.Debug("Sora", "Sending delete_msg request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.RecallMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 处理加好友请求
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="flag">请求flag</param>
        /// <param name="approve">是否同意</param>
        /// <param name="remark">好友备注</param>
        internal static async ValueTask<ApiStatus> SetFriendAddRequest(Guid connection, string flag, bool approve,
                                                                       string remark = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            Log.Debug("Sora", "Sending set_friend_add_request request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetFriendAddRequest,
                ApiParams = new
                {
                    flag,
                    approve,
                    remark
                }
            }, connection);
            Log.Debug("Sora", $"Get set_friend_add_request response {nameof(apiStatus)}={apiStatus.RetCode}");
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
        internal static async ValueTask<ApiStatus> SetGroupAddRequest(Guid connection,
                                                                      string flag,
                                                                      GroupRequestType requestType,
                                                                      bool approve,
                                                                      string reason = null)
        {
            Log.Debug("Sora", "Sending set_group_add_request request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAddRequest,
                ApiParams = new SetGroupAddRequestParams
                {
                    Flag             = flag,
                    GroupRequestType = requestType,
                    Approve          = approve,
                    Reason           = reason
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_add_request response {nameof(apiStatus)}={apiStatus.RetCode}");
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
            Log.Debug("Sora", "Sending set_group_card request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupCard,
                ApiParams = new
                {
                    group_id = groupId,
                    user_id  = userId,
                    card
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_card response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 设置群组专属头衔
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="title">头衔</param>
        internal static async ValueTask<ApiStatus> SetGroupSpecialTitle(Guid connection, long groupId, long userId,
                                                                        string title)
        {
            Log.Debug("Sora", "Sending set_group_special_title request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupSpecialTitle,
                ApiParams = new
                {
                    group_id      = groupId,
                    user_id       = userId,
                    special_title = title,
                    duration      = -1
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_special_title response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 群组踢人
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="rejectRequest">拒绝此人的加群请求</param>
        internal static async ValueTask<ApiStatus> KickGroupMember(Guid connection, long groupId, long userId,
                                                                   bool rejectRequest)
        {
            Log.Debug("Sora", "Sending set_group_kick request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupKick,
                ApiParams = new
                {
                    group_id           = groupId,
                    user_id            = userId,
                    reject_add_request = rejectRequest
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_kick response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 群组单人禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="duration">禁言时长(s)</param>
        internal static async ValueTask<ApiStatus> SetGroupBan(Guid connection, long groupId, long userId,
                                                               long duration)
        {
            Log.Debug("Sora", "Sending set_group_ban request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupBan,
                ApiParams = new
                {
                    group_id = groupId,
                    user_id  = userId,
                    duration
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_ban response {nameof(apiStatus)}={apiStatus.RetCode}");
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
            Log.Debug("Sora", "Sending set_group_whole_ban request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupWholeBan,
                ApiParams = new
                {
                    group_id = groupId,
                    enable
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_whole_ban response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="anonymous">匿名用户对象</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        internal static async ValueTask<ApiStatus> SetAnonymousBan(Guid connection, long groupId, Anonymous anonymous,
                                                                   long duration)
        {
            Log.Debug("Sora", "Sending set_group_anonymous_ban request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                ApiParams = new
                {
                    group_id = groupId,
                    anonymous,
                    duration
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_anonymous_ban response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="anonymousFlag">匿名用户flag</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        internal static async ValueTask<ApiStatus> SetAnonymousBan(Guid connection, long groupId, string anonymousFlag,
                                                                   long duration)
        {
            Log.Debug("Sora", "Sending set_group_anonymous_ban request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                ApiParams = new
                {
                    group_id = groupId,
                    flag     = anonymousFlag,
                    duration
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_anonymous_ban response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="userId">成员id</param>
        /// <param name="groupId">群号</param>
        /// <param name="enable">设置或取消</param>
        internal static async ValueTask<ApiStatus> SetGroupAdmin(Guid connection, long userId, long groupId,
                                                                 bool enable)
        {
            Log.Debug("Sora", "Sending set_group_admin request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAdmin,
                ApiParams = new
                {
                    group_id = groupId,
                    user_id  = userId,
                    enable
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_admin response {nameof(apiStatus)}={apiStatus.RetCode}");
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
            Log.Debug("Sora", "Sending set_group_leave request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupLeave,
                ApiParams = new
                {
                    group_id   = groupId,
                    is_dismiss = dismiss
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_leave response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 重启客户端
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="delay">延迟(ms)</param>
        internal static async ValueTask<ApiStatus> Restart(Guid connection, int delay)
        {
            Log.Debug("Sora", "Sending restart client requset");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.Restart,
                ApiParams = new
                {
                    delay
                }
            }, connection);
            Log.Debug("Sora", $"Get restart response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        #region Go API

        /// <summary>
        /// 设置群名
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="name">新群名</param>
        internal static async ValueTask<ApiStatus> SetGroupName(Guid connection, long groupId, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new NullReferenceException(nameof(name));
            Log.Debug("Sora", "Sending set_group_name request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupName,
                ApiParams = new
                {
                    group_id   = groupId,
                    group_name = name
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_name response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 设置群头像
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="file">图片文件</param>
        /// <param name="useCache">是否使用缓存</param>
        internal static async ValueTask<ApiStatus> SetGroupPortrait(Guid connection, long groupId, string file,
                                                                    bool useCache)
        {
            if (string.IsNullOrEmpty(file)) throw new NullReferenceException(nameof(file));
            Log.Debug("Sora", "Sending set_group_portrait request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupPortrait,
                ApiParams = new
                {
                    group_id = groupId,
                    file,
                    cache = useCache ? 1 : 0
                }
            }, connection);
            Log.Debug("Sora", $"Get set_group_portrait response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 发送合并转发(群)
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="msgList">消息段数组</param>
        internal static async ValueTask<ApiStatus> SendGroupForwardMsg(Guid connection, long groupId,
                                                                       IEnumerable<CustomNode> msgList)
        {
            //将消息段转换为数组
            var customNodes = msgList as CustomNode[] ?? msgList.ToArray();
            if (msgList == null || !customNodes.Any()) throw new NullReferenceException("msgList is null or empty");
            //处理发送消息段
            var dataObj = customNodes.Select(node => new
            {
                type = "node",
                data = node
            }).ToList();

            Log.Debug("Sora", "Sending send_group_forward_msg request");
            //发送消息
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendGroupForwardMsg,
                ApiParams = new
                {
                    group_id = groupId.ToString(),
                    messages = dataObj
                }
            }, connection);
            Log.Debug("Sora", $"Get send_group_forward_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 重载事件过滤器
        /// </summary>
        /// <param name="connection">连接标识</param>
        internal static async ValueTask<ApiStatus> ReloadEventFilter(Guid connection)
        {
            Log.Debug("Sora", "Sending reload_event_filter request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.ReloadEventFilter
            }, connection);
            Log.Debug("Sora", $"Get reload_event_filter response {nameof(apiStatus)}={apiStatus.RetCode}");
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
        internal static async ValueTask<ApiStatus> UploadGroupFile(Guid connection, long groupId, string localFilePath,
                                                                   string fileName,
                                                                   string folderId = null, int timeout = 10000)
        {
            Log.Debug("Sora", "Sending upload_group_file request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.UploadGroupFile,
                ApiParams = new
                {
                    group_id = groupId,
                    file     = localFilePath ?? throw new NullReferenceException("localFilePath is null"),
                    name     = fileName      ?? throw new NullReferenceException("fileName is null"),
                    folder   = folderId      ?? string.Empty
                }
            }, connection, TimeSpan.FromMilliseconds(timeout));
            Log.Debug("Sora", $"Get upload_group_file response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 设置精华消息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<ApiStatus> SetEssenceMsg(Guid connection, long msgId)
        {
            Log.Debug("Sora", "Sending set_essence_msg request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetEssenceMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get set_essence_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 删除精华消息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<ApiStatus> DelEssenceMsg(Guid connection, long msgId)
        {
            Log.Debug("Sora", "Sending delete_essence_msg request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteEssenceMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_essence_msg response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 发送群公告
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="content">公告内容</param>
        /// <param name="image">图片</param>
        internal static async ValueTask<ApiStatus> SendGroupNotice(Guid connection, long groupId, string content,
                                                                   string image)
        {
            Log.Debug("Sora", "Sending _send_group_notice request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._SendGroupNotice,
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
            }, connection);
            Log.Debug("Sora", $"Get _send_group_notice response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="userId">用户id</param>
        internal static async ValueTask<ApiStatus> DeleteFriend(Guid connection, long userId)
        {
            Log.Debug("Sora", "Sending delete_friend request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteFriend,
                ApiParams = new
                {
                    id = userId
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_friend response {nameof(apiStatus)}={apiStatus.RetCode}");
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
            Log.Debug("Sora", "Sending _set_model_show request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._SetModelShow,
                ApiParams = new
                {
                    model,
                    model_show = showModel
                }
            }, connection);
            Log.Debug("Sora", $"Get _set_model_show response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 创建群文件夹
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="name">文件夹名</param>
        /// <param name="folderId">文件夹ID</param>
        //TODO 测试发现folderId似乎无效，无法创建文件夹套文件夹，待gocq后续完善
        internal static async ValueTask<ApiStatus> CreateGroupFileFolder(
            Guid connection, long groupId, string name, string folderId)
        {
            Log.Debug("Sora", "Sending create_group_file_folder request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
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
            }, connection);
            Log.Debug("Sora", $"Get create_group_file_folder response {nameof(apiStatus)}={apiStatus.RetCode}");
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
            Log.Debug("Sora", "Sending delete_group_folder request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteGroupFolder,
                ApiParams = new
                {
                    group_id  = groupId,
                    folder_id = folderId
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_group_folder response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 删除群文件文件夹
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="groupId">群号</param>
        /// <param name="fileId">文件ID</param>
        /// <param name="busId">文件类型</param>
        internal static async ValueTask<ApiStatus> DeleteGroupFile(Guid connection, long groupId, string fileId,
                                                                   int busId)
        {
            Log.Debug("Sora", "Sending delete_group_file request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteGroupFile,
                ApiParams = new
                {
                    group_id = groupId,
                    file_id  = fileId,
                    bus_id   = busId
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_group_file response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }

        /// <summary>
        /// 标记消息已读
        /// </summary>
        /// <param name="connection">连接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<ApiStatus> MarkMessageRead(Guid connection, int msgId)
        {
            Log.Debug("Sora", "Sending mark_msg_as_read request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.MarkMsgAsRead,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            Log.Debug("Sora", $"Get mark_msg_as_read response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }
        
        /// <summary>
        /// 删除单向好友
        /// </summary>
        /// <param name="connection">连接标识</param>
        /// <param name="uid">uid</param>
        internal static async ValueTask<ApiStatus> DeleteUnidirectionalFriend(Guid connection, long uid)
        {
            Log.Debug("Sora", "Sending delete_unidirectional_friend request");
            var (apiStatus, _) = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteUnidirectionalFriend,
                ApiParams = new
                {
                    user_id = uid
                }
            }, connection);
            Log.Debug("Sora", $"Get delete_unidirectional_friend response {nameof(apiStatus)}={apiStatus.RetCode}");
            return apiStatus;
        }
        
        #endregion

        #endregion
    }
}