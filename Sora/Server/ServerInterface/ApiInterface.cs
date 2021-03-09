using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Entities.CQCodes;
using Sora.Enumeration.EventParamsType;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Server.ApiParams;
using Sora.Entities;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using Sora.Server.OnebotEvent.MessageEvent;
using YukariToolBox.FormatLog;

namespace Sora.Server.ServerInterface
{
    internal static class ApiInterface
    {
        #region API请求

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="target">发送目标uid</param>
        /// <param name="groupId">临时会话来源群</param>
        /// <param name="messages">发送的信息</param>
        /// <returns>
        /// message id
        /// </returns>
        internal static async ValueTask<(int retCode, int messageId)> SendPrivateMessage(
            Guid connection, long target, long? groupId, List<CQCode> messages)
        {
            Log.Debug("Sora", "Sending send_msg(Private) request");
            if (messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //转换消息段列表
            var messagesList = messages.Select(msg => msg.ToOnebotMessage()).ToList();
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Private,
                    UserId      = target,
                    Message     = messagesList,
                    GroupId     = groupId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get send_msg(Private) response retcode={retCode}");
            if (retCode != 0) return (retCode, -1);
            var msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out var messageCode)
                ? messageCode
                : -1;
            Log.Debug("Sora", $"msg send -> private[{target}]");
            return (retCode, msgCode);
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
        internal static async ValueTask<(int retCode, int messageId)> SendGroupMessage(
            Guid connection, long target, List<CQCode> messages)
        {
            Log.Debug("Sora", "Sending send_msg(Group) request");
            if (messages == null || messages.Count == 0) throw new NullReferenceException(nameof(messages));
            //转换消息段列表
            var messagesList = messages.Select(msg => msg.ToOnebotMessage()).ToList();
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendMsg,
                ApiParams = new SendMessageParams
                {
                    MessageType = MessageType.Group,
                    GroupId     = target,
                    Message     = messagesList
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get send_msg(Group) response retcode={retCode}");
            if (retCode != 0) return (retCode, -1);
            var msgCode = int.TryParse(ret?["data"]?["message_id"]?.ToString(), out var messageCode)
                ? messageCode
                : -1;
            Log.Debug("Sora", $"msg send -> group[{target}]");
            return (retCode, msgCode);
        }

        /// <summary>
        /// 获取登陆账号信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<(int retCode, long uid, string nick)> GetLoginInfo(Guid connection)
        {
            Log.Debug("Sora", "Sending get_login_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetLoginInfo
            }, connection);
            //处理API返回信息
            int retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_login_info response retcode={retCode}");
            if (retCode != 0) return (retCode, -1, null);
            return
            (
                retCode,
                uid: int.TryParse(ret?["data"]?["user_id"]?.ToString(), out var uid) ? uid : -1,
                nick: ret?["data"]?["nickname"]?.ToString() ?? string.Empty
            );
        }

        /// <summary>
        /// 获取版本信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(int retCode, string clientType, string clientVer)> GetClientInfo(
            Guid connection)
        {
            Log.Debug("Sora", "Sending get_version_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetVersion
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_version_info response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, "unknown", null);
            var verStr = ret["data"]?["version"]?.ToString() ?? ret["data"]?["app_version"]?.ToString() ?? string.Empty;

            return (retCode, ret["data"]?["app_name"]?.ToString() ?? "unknow", verStr);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>好友信息列表</returns>
        internal static async ValueTask<(int retCode, List<FriendInfo> friendList)> GetFriendList(Guid connection)
        {
            Log.Debug("Sora", "Sending get_friend_list request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetFriendList
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_friend_list response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            //处理返回的好友信息
            var friendInfos = ret["data"]?.Select(token => new FriendInfo
            {
                UserId = Convert.ToInt64(token["user_id"] ?? -1),
                Remark = token["remark"]?.ToString()   ?? string.Empty,
                Nick   = token["nickname"]?.ToString() ?? string.Empty
            }).ToList();

            return (retCode, friendInfos);
        }

        /// <summary>
        /// 获取群组列表
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>群组信息列表</returns>
        internal static async ValueTask<(int retCode, List<GroupInfo> groupList)> GetGroupList(Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_list request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupList
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_list response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            //处理返回群组列表
            var groupList = ret["data"].Select(token => new GroupInfo
            {
                GroupId        = Convert.ToInt64(token["group_id"] ?? -1),
                GroupName      = token["group_name"]?.ToString() ?? string.Empty,
                MemberCount    = Convert.ToInt32(token["member_count"]     ?? -1),
                MaxMemberCount = Convert.ToInt32(token["max_member_count"] ?? -1)
            }).ToList();

            return (retCode, groupList);
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        internal static async ValueTask<(int retCode, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList(
            Guid connection, long gid)
        {
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupMemberList,
                ApiParams = new
                {
                    group_id = gid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_member_list response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            //处理返回群成员列表
            return (retCode,
                    ret["data"]?.ToObject<List<GroupMemberInfo>>());
        }

        /// <summary>
        /// 获取群信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="useCache">是否使用缓存</param>
        internal static async ValueTask<(int retCode, GroupInfo groupInfo)> GetGroupInfo(
            Guid connection, long gid, bool useCache)
        {
            Log.Debug("Sora", "Sending get_group_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupInfo,
                ApiParams = new
                {
                    group_id = gid,
                    no_cache = !useCache
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_info response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, new GroupInfo());
            return (retCode,
                    new GroupInfo
                    {
                        GroupId        = Convert.ToInt64(ret["data"]["group_id"] ?? -1),
                        GroupName      = ret["data"]["group_name"]?.ToString() ?? string.Empty,
                        MemberCount    = Convert.ToInt32(ret["data"]["member_count"]     ?? -1),
                        MaxMemberCount = Convert.ToInt32(ret["data"]["max_member_count"] ?? -1)
                    }
                );
        }

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="uid">用户ID</param>
        /// <param name="useCache">是否使用缓存</param>
        internal static async ValueTask<(int retCode, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
            Guid connection, long gid, long uid, bool useCache)
        {
            Log.Debug("Sora", "Sending get_group_member_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupMemberInfo,
                ApiParams = new
                {
                    group_id = gid,
                    user_id  = uid,
                    no_cache = !useCache
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_member_info response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, new GroupMemberInfo());
            return (retCode,
                    ret["data"]?.ToObject<GroupMemberInfo>() ?? new GroupMemberInfo());
        }

        /// <summary>
        /// 获取用户信息
        /// 可以为好友或陌生人
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="uid">用户ID</param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        internal static async ValueTask<(int retCode, UserInfo userInfo, string qid)> GetUserInfo(
            Guid connection, long uid, bool useCache)
        {
            Log.Debug("Sora", "Sending get_stranger_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetStrangerInfo,
                ApiParams = new
                {
                    user_id  = uid,
                    no_cache = !useCache
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_stranger_info response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, new UserInfo(), string.Empty);
            return (retCode, new UserInfo
            {
                UserId    = Convert.ToInt64(ret["data"]?["user_id"] ?? -1),
                Nick      = ret["data"]?["nickname"]?.ToString(),
                Age       = Convert.ToInt32(ret["data"]?["age"] ?? -1),
                Sex       = ret["data"]?["sex"]?.ToString(),
                Level     = Convert.ToInt32(ret["data"]?["level"]      ?? -1),
                LoginDays = Convert.ToInt32(ret["data"]?["login_days"] ?? -1)
            }, ret["data"]?["qid"]?.ToString());
        }

        /// <summary>
        /// 检查是否可以发送图片
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(int retCode, bool canSend)> CanSendImage(Guid connection)
        {
            Log.Debug("Sora", "Sending can_send_image request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CanSendImage
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get can_send_image response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, false);
            return (retCode,
                    Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
        }

        /// <summary>
        /// 检查是否可以发送语音
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(int retCode, bool canSend)> CanSendRecord(Guid connection)
        {
            Log.Debug("Sora", "Sending can_send_record request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CanSendRecord
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get can_send_record response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, false);
            return (retCode,
                    Convert.ToBoolean(ret["data"]?["yes"]?.ToString() ?? "false"));
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        internal static async ValueTask<(int retCode, bool online, bool good, JObject statData)> GetStatus(
            Guid connection)
        {
            Log.Debug("Sora", "Sending get_status request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetStatus
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_status response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, false, false, null);
            return (retCode,
                    Convert.ToBoolean(ret["data"]?["online"]?.ToString() ?? "false"),
                    Convert.ToBoolean(ret["data"]?["good"]?.ToString()   ?? "false"),
                    JObject.FromObject(ret["data"]?["stat"]              ?? ret["data"]));
        }

        #region Go API

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="cacheFileName">缓存文件名</param>
        internal static async ValueTask<(int retCode, int size, string fileName, string url)> GetImage(
            Guid connection, string cacheFileName)
        {
            Log.Debug("Sora", "Sending get_image request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetImage,
                ApiParams = new
                {
                    file = cacheFileName
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_image response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, -1, null, null);
            return (retCode,
                    Convert.ToInt32(ret["data"]?["size"] ?? 1),
                    ret["data"]?["filename"]?.ToString(),
                    ret["data"]?["url"]?.ToString());
        }

        /// <summary>
        /// 获取消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<(int retCode, Message message, User sender, Group sourceGroup, int realId, bool
            isGroupMsg)> GetMessage(
            Guid connection, int msgId)
        {
            Log.Debug("Sora", "Sending get_msg request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetMessage,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_msg response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null, null, null, 0, false);
            //处理消息段
            var rawMessage = ret["data"]?["message"]?.ToObject<List<MessageElement>>();
            return (retCode,
                    new Message(connection,
                                msgId,
                                ret["data"]?["raw_message"]?.ToString(),
                                MessageParse.Parse(rawMessage        ?? new List<MessageElement>()),
                                Convert.ToInt64(ret["data"]?["time"] ?? -1),
                                0,
                                Convert.ToBoolean(ret["data"]?["group"]           ?? false)
                                    ? Convert.ToInt32(ret["data"]?["message_seq"] ?? 0)
                                    : null),
                    new User(connection,
                             Convert.ToInt64(ret["data"]?["sender"]?["user_id"] ?? -1)),
                    //判断响应数据中是否有群组信息
                    Convert.ToBoolean(ret["data"]?["group"] ?? false)
                        ? new Group(connection, Convert.ToInt64(ret["data"]?["group_id"] ?? 0))
                        : null,
                    Convert.ToInt32(ret["data"]?["real_id"]                                    ?? 0),
                    Convert.ToBoolean(ret["data"]?["message_type"]?.ToString().Equals("group") ?? false));
        }

        /// <summary>
        /// 获取中文分词
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="text">内容</param>
        /// <returns>词组列表</returns>
        internal static async ValueTask<(int retCode, List<string> slicesList)> GetWordSlices(
            Guid connection, string text)
        {
            if (string.IsNullOrEmpty(text)) throw new NullReferenceException(nameof(text));
            Log.Debug("Sora", "Sending .get_word_slices request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetWordSlices,
                ApiParams = new
                {
                    content = text
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get .get_word_slices response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            return (retCode,
                    ret["data"]?["slices"]?.ToObject<List<string>>());
        }

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">合并转发 ID</param>
        /// <returns>ApiResponseCollection</returns>
        internal static async ValueTask<(int retCode, NodeArray nodeArray)> GetForwardMessage(
            Guid connection, string msgId)
        {
            if (string.IsNullOrEmpty(msgId)) throw new NullReferenceException(nameof(msgId));
            Log.Debug("Sora", "Sending get_forward_msg request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetForwardMessage,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_forward_msg response retcode={retCode}");
            if (retCode != 0) return (retCode, null);
            //转换消息类型
            var messageList = ret?["data"]?.ToObject<NodeArray>() ?? new NodeArray();
            messageList.ParseNode();
            return (retCode, messageList);
        }

        /// <summary>
        /// 获取群系统消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <returns>消息列表</returns>
        internal static async
            ValueTask<(int retCode, List<GroupRequestInfo> joinList, List<GroupRequestInfo> invitedList)>
            GetGroupSystemMsg(Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_system_msg request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupSystemMsg
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_system_msg response retcode={retCode}");
            if (retCode != 0)
                return (retCode, new List<GroupRequestInfo>(), new List<GroupRequestInfo>());
            //解析消息
            var joinList =
                ret?["data"]?["join_requests"]?.ToObject<List<GroupRequestInfo>>() ??
                new List<GroupRequestInfo>();
            var invitedList =
                ret?["data"]?["invited_requests"]?.ToObject<List<GroupRequestInfo>>() ??
                new List<GroupRequestInfo>();
            return (retCode, joinList, invitedList);
        }

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        /// <param name="gid">群号</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文件系统信息</returns>
        internal static async
            ValueTask<(int retCode, GroupFileSysInfo fileSysInfo)> GetGroupFileSysInfo(long gid, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_file_system_info request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFileSystemInfo,
                ApiParams = new
                {
                    group_id = gid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_file_system_info response retcode={retCode}");
            return retCode != 0
                ? (retCode, new GroupFileSysInfo())
                : (retCode, ret?["data"]?.ToObject<GroupFileSysInfo>() ?? new GroupFileSysInfo());
            //解析消息
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="gid">群号</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文件列表/文件夹列表</returns>
        internal static async
            ValueTask<(int retCode, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupRootFiles(long gid, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_root_files request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupRootFiles,
                ApiParams = new
                {
                    group_id = gid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_root_files response retcode={retCode}");
            if (retCode != 0)
                return (retCode, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
            return (retCode,
                    ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>()     ?? new List<GroupFileInfo>(),
                    ret?["data"]?["folders"]?.ToObject<List<GroupFolderInfo>>() ?? new List<GroupFolderInfo>());
        }

        /// <summary>
        /// 获取群子目录文件列表
        /// </summary>
        /// <param name="gid">群号</param>
        /// <param name="folderID">文件夹ID</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文件列表/文件夹列表</returns>
        internal static async
            ValueTask<(int retCode, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupFilesByFolder(long gid, string folderID, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_files_by_folder request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFilesByFolder,
                ApiParams = new
                {
                    group_id  = gid,
                    folder_id = folderID
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_files_by_folder response retcode={retCode}");
            if (retCode != 0)
                return (retCode, new List<GroupFileInfo>(), new List<GroupFolderInfo>());
            return (retCode,
                    ret?["data"]?["files"]?.ToObject<List<GroupFileInfo>>()     ?? new List<GroupFileInfo>(),
                    ret?["data"]?["folders"]?.ToObject<List<GroupFolderInfo>>() ?? new List<GroupFolderInfo>());
        }

        /// <summary>
        /// 获取群文件资源链接
        /// </summary>
        /// <param name="gid">群号</param>
        /// <param name="fileId">文件ID</param>
        /// <param name="busId">文件类型</param>
        /// <param name="connection">连接标识</param>
        /// <returns>资源链接</returns>
        internal static async ValueTask<(int retCode, string fileUrl)> GetGroupFileUrl(
            long gid, string fileId, int busId, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_file_url request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupFileUrl,
                ApiParams = new
                {
                    group_id = gid,
                    file_id  = fileId,
                    busid    = busId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_file_url response retcode={retCode}");
            return retCode != 0
                ? (retCode, null)
                : (retCode, ret?["data"]?["url"]?.ToString());
        }

        /// <summary>
        /// 获取群@全体成员剩余次数
        /// </summary>
        /// <param name="gid">群号</param>
        /// <param name="connection">连接标识</param>
        /// <returns>配额信息</returns>
        internal static async ValueTask<(int retCode, bool canAt, short groupRemain, short botRemain)>
            GetGroupAtAllRemain(long gid, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_at_all_remain request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupAtAllRemain,
                ApiParams = new
                {
                    group_id = gid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_at_all_remain response retcode={retCode}");
            if (retCode != 0)
                return (retCode, false, -1, -1);
            else
                return (retCode, Convert.ToBoolean(ret?["data"]?["can_at_all"]),
                        Convert.ToInt16(ret?["data"]?["remain_at_all_count_for_group"]),
                        Convert.ToInt16(ret?["data"]?["remain_at_all_count_for_uin"]));
        }

        /// <summary>
        /// 图片 OCR
        /// </summary>
        /// <param name="imgId">图片ID</param>
        /// <param name="connection">连接标识</param>
        /// <returns>文字识别信息</returns>
        internal static async ValueTask<(int retCode, List<TextDetection> texts, string lang)> OcrImage(
            string imgId, Guid connection)
        {
            Log.Debug("Sora", "Sending ocr_image request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.Ocr,
                ApiParams = new
                {
                    image = imgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get ocr_image response retcode={retCode}");

            if (retCode != 0)
                return (retCode, new List<TextDetection>(), string.Empty);
            else
                return (retCode, ret?["data"]?["texts"]?.ToObject<List<TextDetection>>(),
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
        internal static async ValueTask<(int retCode, string filePath)> DownloadFile(
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
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DownloadFile,
                ApiParams = new
                {
                    url,
                    thread_count = threadCount,
                    headers      = customHeaderStr
                }
            }, connection, timeout);

            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get download_file response retcode={retCode}");
            return retCode != 0 ? (retCode, string.Empty) : (retCode, ret?["data"]?["file"]?.ToString());
        }

        /// <summary>
        /// 获取群消息历史记录
        /// </summary>
        /// <param name="msgSeq">消息序号*</param>
        /// <param name="gid">群号</param>
        /// <param name="connection">连接标识</param>
        /// <returns>消息</returns>
        internal static async ValueTask<(int retCode, List<GroupMessageEventArgs> msgList)> GetGroupMessageHistory(
            int? msgSeq, long gid, Guid connection)
        {
            Log.Debug("Sora", "Sending get_group_msg_history request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetGroupMsgHistory,
                ApiParams = msgSeq == null
                    ? new
                    {
                        group_id = gid
                    }
                    : new
                    {
                        message_seq = msgSeq,
                        group_id    = gid
                    }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_group_msg_history response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            //处理消息段
            return (0, ret["data"]?["messages"]?.ToObject<List<ApiGroupMsgEventArgs>>()
                                               ?.Select(messageArg =>
                                                            new GroupMessageEventArgs(connection, "group",
                                                                messageArg)).ToList());
        }

        /// <summary>
        /// 获取当前账号在线客户端列表
        /// </summary>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="connection">连接标识</param>
        /// <returns>在线客户端信息</returns>
        internal static async ValueTask<(int retCode, List<ClientInfo> clients)> GetOnlineClients(
            bool useCache, Guid connection)
        {
            Log.Debug("Sora", "Sending get_online_clients request");
            //发送信息
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetOnlineClients,
                ApiParams = new
                {
                    no_cache = !useCache
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_online_clients response retcode={retCode}");
            if (retCode != 0 || ret?["data"] == null) return (retCode, null);
            //处理客户端信息
            return (retCode, ret["data"]?["clients"]?.ToObject<List<ClientInfo>>() ?? new List<ClientInfo>());
        }

        /// <summary>
        /// 获取群精华消息列表
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="gid">群号</param>
        internal static async ValueTask<(int retCode, List<EssenceInfo> essenceInfos)> GetEssenceMsgList(
            Guid connection, long gid)
        {
            Log.Debug("Sora", "Sending get_essence_msg_list request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.GetEssenceMsgList,
                ApiParams = new
                {
                    group_id = gid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get get_essence_msg_list response retcode={retCode}");
            return (retCode, (ret?["data"] ?? new JArray())
                             .Select(element => new EssenceInfo(element, connection)).ToList());
        }

        /// <summary>
        /// 检查链接安全性
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="url">需要检查的链接</param>
        internal static async ValueTask<(int retCode, SecurityLevelType securityLevel)> CheckUrlSafely(
            Guid connection, string url)
        {
            Log.Debug("Sora", "Sending check_url_safely request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.CheckUrlSafely,
                ApiParams = new
                {
                    url
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get check_url_safely response retcode={retCode}");
            return (retCode, (SecurityLevelType) Convert.ToInt32(ret?["data"]?["level"] ?? 1));
        }

        /// <summary>
        /// 获取vip信息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="uid">需要检查的链接</param>
        [Obsolete]
        internal static async ValueTask<(int retCode, VipInfo securityLevel)> GetVipInfo(Guid connection, long uid)
        {
            Log.Debug("Sora", "Sending _get_vip_info request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._GetVipInfo,
                ApiParams = new
                {
                    user_id = uid
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get _get_vip_info response retcode={retCode}");
            return (retCode, ret?["data"]?.ToObject<VipInfo>() ?? new VipInfo());
        }

        #endregion

        #endregion

        #region 无响应数据的API请求

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="msgId">消息id</param>
        internal static async ValueTask<int> RecallMsg(Guid connection, int msgId)
        {
            Log.Debug("Sora", "Sending delete_msg request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.RecallMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get delete_msg response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 处理加好友请求
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="flag">请求flag</param>
        /// <param name="approve">是否同意</param>
        /// <param name="remark">好友备注</param>
        internal static async ValueTask<int> SetFriendAddRequest(Guid connection, string flag, bool approve,
                                                                 string remark = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            Log.Debug("Sora", "Sending set_friend_add_request request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetFriendAddRequest,
                ApiParams = new
                {
                    flag,
                    approve,
                    remark
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_friend_add_request response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 处理加群请求/邀请
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="flag">请求flag</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="approve">是否同意</param>
        /// <param name="reason">好友备注</param>
        internal static async ValueTask<int> SetGroupAddRequest(Guid connection,
                                                                string flag,
                                                                GroupRequestType requestType,
                                                                bool approve,
                                                                string reason = null)
        {
            Log.Debug("Sora", "Sending set_group_add_request request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
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
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_add_request response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 设置群名片
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="uid">用户id</param>
        /// <param name="card">新名片</param>
        internal static async ValueTask<int> SetGroupCard(Guid connection, long gid, long uid, string card)
        {
            Log.Debug("Sora", "Sending set_group_card request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupCard,
                ApiParams = new
                {
                    group_id = gid,
                    user_id  = uid,
                    card
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_card response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 设置群组专属头衔
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="uid">用户id</param>
        /// <param name="title">头衔</param>
        internal static async ValueTask<int> SetGroupSpecialTitle(Guid connection, long gid, long uid, string title)
        {
            Log.Debug("Sora", "Sending set_group_special_title request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupSpecialTitle,
                ApiParams = new
                {
                    group_id      = gid,
                    user_id       = uid,
                    special_title = title,
                    duration      = -1
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_special_title response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 群组踢人
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="uid">用户id</param>
        /// <param name="rejectRequest">拒绝此人的加群请求</param>
        internal static async ValueTask<int> KickGroupMember(Guid connection, long gid, long uid, bool rejectRequest)
        {
            Log.Debug("Sora", "Sending set_group_kick request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupKick,
                ApiParams = new
                {
                    group_id           = gid,
                    user_id            = uid,
                    reject_add_request = rejectRequest
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_kick response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 群组单人禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="uid">用户id</param>
        /// <param name="duration">禁言时长(s)</param>
        internal static async ValueTask<int> SetGroupBan(Guid connection, long gid, long uid, long duration)
        {
            Log.Debug("Sora", "Sending set_group_ban request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupBan,
                ApiParams = new
                {
                    group_id = gid,
                    user_id  = uid,
                    duration
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_ban response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 群组全员禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="enable">是否禁言</param>
        internal static async ValueTask<int> SetGroupWholeBan(Guid connection, long gid, bool enable)
        {
            Log.Debug("Sora", "Sending set_group_whole_ban request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupWholeBan,
                ApiParams = new
                {
                    group_id = gid,
                    enable
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_whole_ban response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="anonymous">匿名用户对象</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        internal static async ValueTask<int> SetAnonymousBan(Guid connection, long gid, Anonymous anonymous,
                                                             long duration)
        {
            Log.Debug("Sora", "Sending set_group_anonymous_ban request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                ApiParams = new
                {
                    group_id = gid,
                    anonymous,
                    duration
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_anonymous_ban response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="anonymousFlag">匿名用户flag</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        internal static async ValueTask<int> SetAnonymousBan(Guid connection, long gid, string anonymousFlag,
                                                             long duration)
        {
            Log.Debug("Sora", "Sending set_group_anonymous_ban request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAnonymousBan,
                ApiParams = new
                {
                    group_id = gid,
                    flag     = anonymousFlag,
                    duration
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_anonymous_ban response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="uid">成员id</param>
        /// <param name="gid">群号</param>
        /// <param name="enable">设置或取消</param>
        internal static async ValueTask<int> SetGroupAdmin(Guid connection, long uid, long gid, bool enable)
        {
            Log.Debug("Sora", "Sending set_group_admin request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupAdmin,
                ApiParams = new
                {
                    group_id = gid,
                    user_id  = uid,
                    enable
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_admin response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 退出/解散群
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="dismiss">是否解散</param>
        internal static async ValueTask<int> SetGroupLeave(Guid connection, long gid, bool dismiss)
        {
            Log.Debug("Sora", "Sending set_group_leave request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupLeave,
                ApiParams = new
                {
                    group_id   = gid,
                    is_dismiss = dismiss
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_leave response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 重启客户端
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="delay">延迟(ms)</param>
        internal static async ValueTask<int> Restart(Guid connection, int delay)
        {
            Log.Debug("Sora", "Sending restart client requset");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.Restart,
                ApiParams = new
                {
                    delay
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get restart response retcode={retCode}");
            return retCode;
        }

        #region Go API

        /// <summary>
        /// 设置群名
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="name">新群名</param>
        internal static async ValueTask<int> SetGroupName(Guid connection, long gid, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new NullReferenceException(nameof(name));
            Log.Debug("Sora", "Sending set_group_name request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupName,
                ApiParams = new
                {
                    group_id   = gid,
                    group_name = name
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_name response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 设置群头像
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="file">图片文件</param>
        /// <param name="useCache">是否使用缓存</param>
        internal static async ValueTask<int> SetGroupPortrait(Guid connection, long gid, string file, bool useCache)
        {
            if (string.IsNullOrEmpty(file)) throw new NullReferenceException(nameof(file));
            Log.Debug("Sora", "Sending set_group_portrait request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetGroupPortrait,
                ApiParams = new
                {
                    group_id = gid,
                    file,
                    cache = useCache ? 1 : 0
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_group_portrait response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 发送合并转发(群)
        /// </summary>
        /// <param name="connection">服务器连接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="msgList">消息段数组</param>
        internal static async ValueTask<int> SendGroupForwardMsg(Guid connection, long gid,
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
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SendGroupForwardMsg,
                ApiParams = new
                {
                    group_id = gid.ToString(),
                    messages = dataObj
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get send_group_forward_msg response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 重载事件过滤器
        /// </summary>
        /// <param name="connection">连接标识</param>
        internal static async ValueTask<int> ReloadEventFilter(Guid connection)
        {
            Log.Debug("Sora", "Sending reload_event_filter request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.ReloadEventFilter
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get reload_event_filter response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 上传群文件
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="fileName">上传文件名</param>
        /// <param name="floderId">父目录ID 为<see langword="null"/>时则上传到根目录</param>
        /// <param name="timeout">超时</param>
        internal static async ValueTask<int> UploadGroupFile(Guid connection, long gid, string localFilePath,
                                                             string fileName,
                                                             string floderId = null, int timeout = 10000)
        {
            Log.Debug("Sora", "Sending upload_group_file request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.UploadGroupFile,
                ApiParams = new
                {
                    group_id = gid,
                    file     = localFilePath ?? throw new NullReferenceException("localFilePath is null"),
                    name     = fileName      ?? throw new NullReferenceException("fileName is null"),
                    folder   = floderId      ?? string.Empty
                }
            }, connection, timeout);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get upload_group_file response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 设置精华消息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<int> SetEssenceMsg(Guid connection, long msgId)
        {
            Log.Debug("Sora", "Sending set_essence_msg request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.SetEssenceMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get set_essence_msg response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 删除精华消息
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="msgId">消息ID</param>
        internal static async ValueTask<int> DelEssenceMsg(Guid connection, long msgId)
        {
            Log.Debug("Sora", "Sending delete_essence_msg request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType.DeleteEssenceMsg,
                ApiParams = new
                {
                    message_id = msgId
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get delete_essence_msg response retcode={retCode}");
            return retCode;
        }

        /// <summary>
        /// 发送群公告
        /// </summary>
        /// <param name="connection">链接标识</param>
        /// <param name="gid">群号</param>
        /// <param name="content">公告内容</param>
        internal static async ValueTask<int> SendGroupNotice(Guid connection, long gid, string content)
        {
            Log.Debug("Sora", "Sending _send_group_notice request");
            var ret = await ReactiveApiManager.SendApiRequest(new ApiRequest
            {
                ApiRequestType = ApiRequestType._SendGroupNotice,
                ApiParams = new
                {
                    group_id = gid,
                    content
                }
            }, connection);
            //处理API返回信息
            var retCode = GetBaseRetCode(ret).retCode;
            Log.Debug("Sora", $"Get _send_group_notice response retcode={retCode}");
            return retCode;
        }

        #endregion

        #endregion

        #region 数据处理

        /// <summary>
        /// 获取API状态返回值
        /// 所有API回调请求都会返回状态值
        /// </summary>
        /// <param name="msg">消息JSON</param>
        private static (int retCode, string status) GetBaseRetCode(JObject msg)
        {
            if (msg == null) return (retCode: -1, status: "failed");
            return
            (
                retCode: int.TryParse(msg["retcode"]?.ToString(), out var messageCode) ? messageCode : -1,
                status: msg["status"]?.ToString() ?? "failed"
            );
        }

        #endregion
    }
}