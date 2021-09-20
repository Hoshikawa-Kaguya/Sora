using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Entities.Segment.DataModel;
using Sora.Entities.Info;
using Sora.Entities.Segment;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Net;
using Sora.OnebotInterface;

namespace Sora.Entities.Base
{
    /// <summary>
    /// Sora API类
    /// </summary>
    public sealed class SoraApi
    {
        #region 属性

        /// <summary>
        /// 当前实例对应的链接ID
        /// 用于调用API
        /// </summary>
        internal Guid ConnectionId { get; }

        /// <summary>
        /// 当前实例对应的服务ID
        /// </summary>
        internal Guid ServiceId { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化Api实例
        /// </summary>
        /// <param name="serviceId">服务ID</param>
        /// <param name="connectionId">连接ID</param>
        internal SoraApi(Guid serviceId, Guid connectionId)
        {
            ServiceId    = serviceId;
            ConnectionId = connectionId;
        }

        #endregion

        #region 通讯类API

        #region 消息API

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="userId">发送目标群id</param>
        /// <param name="message">消息内容</param>
        /// <param name="timeout">覆盖原有超时</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, int messageId)> SendPrivateMessage(
            long userId, MessageBody message, TimeSpan? timeout = null)
        {
            if (userId        < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            if (message.Count == 0) throw new NullReferenceException(nameof(message));
            return await ApiInterface.SendPrivateMessage(ConnectionId, userId, message, null, timeout);
        }

        /// <summary>
        /// 发起群临时会话（私聊）
        /// </summary>
        /// <param name="userId">发送目标群id</param>
        /// <param name="groupId">群号</param>
        /// <param name="message">消息内容</param>
        /// <param name="timeout">覆盖原有超时</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, int messageId)> SendTemporaryMessage(
            long userId, long groupId, MessageBody message, TimeSpan? timeout = null)
        {
            if (userId        < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            if (groupId       < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (message.Count == 0) throw new NullReferenceException(nameof(message));
            return await ApiInterface.SendPrivateMessage(ConnectionId, userId, message, groupId, timeout);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="groupId">发送目标群id</param>
        /// <param name="message">消息内容</param>
        /// <param name="timeout">覆盖原有超时</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, int messageId)> SendGroupMessage(
            long groupId, MessageBody message, TimeSpan? timeout = null)
        {
            if (groupId       < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (message.Count == 0) throw new NullReferenceException(nameof(message));
            return await ApiInterface.SendGroupMessage(ConnectionId, groupId, message, timeout);
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        public async ValueTask<ApiStatus> RecallMessage(int messageId)
        {
            return await ApiInterface.RecallMsg(ConnectionId, messageId);
        }

        #region GoAPI

        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="forwardId">合并转发 ID</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="nodeArray"/> 消息节点列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<Node> nodeArray)> GetForwardMessage(string forwardId)
        {
            if (string.IsNullOrEmpty(forwardId)) throw new NullReferenceException(nameof(forwardId));
            var (apiStatus, nodeArray) = await ApiInterface.GetForwardMessage(ConnectionId, forwardId);
            return (apiStatus, nodeArray);
        }

        /// <summary>
        /// 发送合并转发(群)
        /// 但好像不能用的样子
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="nodeList">
        /// 节点(<see cref="CustomNode"/>)消息段列表
        /// </param>
        public async ValueTask<ApiStatus> SendGroupForwardMsg(long groupId, IEnumerable<CustomNode> nodeList)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            IEnumerable<CustomNode> customNodes = nodeList as CustomNode[] ?? nodeList.ToArray();
            if (nodeList == null || !customNodes.Any()) throw new NullReferenceException(nameof(nodeList));
            return await ApiInterface.SendGroupForwardMsg(ConnectionId, groupId, customNodes);
        }

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="cacheFileName">缓存文件名</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="size"/> 文件大小(Byte)</para>
        /// <para><see langword="fileName"/> 文件名</para>
        /// <para><see langword="url"/> 文件链接</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, int size, string fileName, string url)> GetImage(
            string cacheFileName)
        {
            if (string.IsNullOrEmpty(cacheFileName)) throw new ArgumentOutOfRangeException(nameof(cacheFileName));
            return await ApiInterface.GetImage(ConnectionId, cacheFileName);
        }

        /// <summary>
        /// <para>获取群消息</para>
        /// <para>只能获取纯文本信息</para>
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="Message"/> 消息内容</para>
        /// <para><see cref="User"/> 发送者</para>
        /// <para><see cref="Group"/> 消息来源群，如果不是群消息则为<see langword="null"/></para>
        /// </returns>
        public async
            ValueTask<(ApiStatus apiStatus, Message message, User sender, Group sourceGroup,
                int realId, bool isGroupMsg)> GetMessage(int messageId)
        {
            return await ApiInterface.GetMessage(ServiceId, ConnectionId, messageId);
        }

        /// <summary>
        /// <para>获取群消息历史记录</para>
        /// <para>能获取起始消息的前19条消息</para>
        /// </summary>
        /// <param name="messageSequence">起始消息序号，为<see langword="null"/>时默认从最新消息拉取</param>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 消息记录</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<GroupMessageEventArgs> messages)> GetGroupMessageHistory(
            long groupId, long? messageSequence = null)
        {
            return await ApiInterface.GetGroupMessageHistory(messageSequence, groupId, ServiceId, ConnectionId);
        }

        /// <summary>
        /// 获取当前账号在线客户端列表
        /// </summary>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 在线客户端信息列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<ClientInfo> clients)> GetOnlineClients(bool useCache = true)
        {
            return await ApiInterface.GetOnlineClients(useCache, ConnectionId);
        }

        /// <summary>
        /// 标记消息已读
        /// </summary>
        /// <param name="messageId">消息ID</param>
        public async ValueTask<ApiStatus> MarkMessageRead(int messageId)
        {
            return await ApiInterface.MarkMessageRead(ConnectionId, messageId);
        }

        #endregion

        #endregion

        #region 群管理方法

        /// <summary>
        /// 设置群名片
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="card">
        /// <para>新名片</para>
        /// <para>当值为 <see langword="null"/> 或 <see cref="string"/>.<see langword="Empty"/> 时为清空名片</para>
        /// </param>
        public async ValueTask<ApiStatus> SetGroupCard(long groupId, long userId, string card)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.SetGroupCard(ConnectionId, groupId, userId, card);
        }

        /// <summary>
        /// 设置群成员专属头衔
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="specialTitle">专属头衔(为空时清空)</param>
        public async ValueTask<ApiStatus> SetGroupMemberSpecialTitle(long groupId, long userId, string specialTitle)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.SetGroupSpecialTitle(ConnectionId, groupId, userId,
                                                           specialTitle);
        }

        /// <summary>
        /// 群组踢人
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="rejectRequest">拒绝此人的加群请求</param>
        public async ValueTask<ApiStatus> KickGroupMember(long groupId, long userId, bool rejectRequest)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.KickGroupMember(ConnectionId, groupId, userId,
                                                      rejectRequest);
        }

        /// <summary>
        /// 设置群组成员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="duration">
        /// <para>禁言时长(s)</para>
        /// <para>至少60s</para>
        /// </param>
        public async ValueTask<ApiStatus> EnableGroupMemberMute(long groupId, long userId, long duration)
        {
            if (groupId < 100000 || userId < 10000 || duration < 60)
                throw new
                    ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} or {nameof(duration)} out of range");
            return await ApiInterface.SetGroupBan(ConnectionId, groupId, userId, duration);
        }

        /// <summary>
        /// 解除群组成员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        public async ValueTask<ApiStatus> DisableGroupMemberMute(long groupId, long userId)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.SetGroupBan(ConnectionId, groupId, userId, 0);
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="anonymous">匿名用户对象</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        public async ValueTask<ApiStatus> EnableGroupAnonymousMute(long groupId, Anonymous anonymous, long duration)
        {
            if (groupId < 100000 || duration < 60)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(duration)} out of range");
            if (anonymous == null)
                throw new NullReferenceException("anonymous null");
            return await ApiInterface.SetAnonymousBan(ConnectionId, groupId, anonymous,
                                                      duration);
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="anonymousFlag">匿名用户Flag</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        public async ValueTask<ApiStatus> EnableGroupAnonymousMute(long groupId, string anonymousFlag,
                                                                   long duration)
        {
            if (groupId < 100000 || duration < 60)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(duration)} out of range");
            if (anonymousFlag == null)
                throw new NullReferenceException("anonymousFlag null");
            return await ApiInterface.SetAnonymousBan(ConnectionId, groupId, anonymousFlag,
                                                      duration);
        }

        /// <summary>
        /// 群组全员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask<ApiStatus> EnableGroupMute(long groupId)
        {
            if (groupId < 100000)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.SetGroupWholeBan(ConnectionId, groupId, true);
        }

        /// <summary>
        /// 解除群组全员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask<ApiStatus> DisableGroupMute(long groupId)
        {
            if (groupId < 100000)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.SetGroupWholeBan(ConnectionId, groupId, false);
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask<ApiStatus> EnableGroupAdmin(long groupId, long userId)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.SetGroupAdmin(ConnectionId, userId, groupId, true);
        }

        /// <summary>
        /// 取消群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask<ApiStatus> DisableGroupAdmin(long groupId, long userId)
        {
            if (groupId < 100000 || userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.SetGroupAdmin(ConnectionId, userId, groupId, false);
        }

        /// <summary>
        /// 退出群
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask<ApiStatus> LeaveGroup(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.SetGroupLeave(ConnectionId, groupId, false);
        }

        /// <summary>
        /// 解散群
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask<ApiStatus> DismissGroup(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.SetGroupLeave(ConnectionId, groupId, true);
        }

        #region GoAPI

        /// <summary>
        /// 设置群名
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="newName">新群名</param>
        public async ValueTask<ApiStatus> SetGroupName(long groupId, string newName)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (string.IsNullOrEmpty(newName)) throw new NullReferenceException(nameof(newName));
            return await ApiInterface.SetGroupName(ConnectionId, groupId, newName);
        }

        /// <summary>
        /// 设置群头像
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="imageFile">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用缓存</param>
        public async ValueTask<ApiStatus> SetGroupPortrait(long groupId, string imageFile, bool useCache = true)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (string.IsNullOrEmpty(imageFile)) throw new NullReferenceException(nameof(imageFile));
            var (retFileStr, isMatch) = SegmentBuilder.ParseDataStr(imageFile);
            if (!isMatch) throw new NotSupportedException($"not supported file type({imageFile})");
            return await ApiInterface.SetGroupPortrait(ConnectionId, groupId, retFileStr,
                                                       useCache);
        }

        /// <summary>
        /// 获取群组系统消息
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="joinList"/> 进群消息列表</para>
        /// <para><see langword="invitedList"/> 邀请消息列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<GroupRequestInfo> joinList, List<GroupRequestInfo>
                invitedList)>
            GetGroupSystemMsg()
        {
            return await ApiInterface.GetGroupSystemMsg(ConnectionId);
        }

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="GroupFileSysInfo"/> 文件系统信息</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, GroupFileSysInfo groupFileSysInfo)> GetGroupFileSysInfo(
            long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.GetGroupFileSysInfo(groupId, ConnectionId);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(ApiStatus apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupRootFiles(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.GetGroupRootFiles(groupId, ConnectionId);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="foldId">文件夹ID</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(ApiStatus apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupFilesByFolder(long groupId, string foldId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.GetGroupFilesByFolder(groupId, foldId, ConnectionId);
        }

        /// <summary>
        /// 获取群@全体成员剩余次数
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="canAt"/> 是否可以@全体成员</para>
        /// <para><see langword="groupRemain"/> 群内所有管理当天剩余@全体成员次数</para>
        /// <para><see langword="botRemain"/> BOT当天剩余@全体成员次数</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, bool canAt, short groupRemain, short botRemain)>
            GetGroupAtAllRemain(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.GetGroupAtAllRemain(groupId, ConnectionId);
        }

        /// <summary>
        /// 获取群文件资源链接
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="fileId">文件ID</param>
        /// <param name="busid">文件类型</param>
        /// <returns>文件链接</returns>
        public async ValueTask<(ApiStatus apiStatus, string fileUrl)> GetGroupFileUrl(
            long groupId, string fileId, int busid)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.GetGroupFileUrl(groupId, fileId, busid, ConnectionId);
        }

        /// <summary>
        /// 上传群文件
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="fileName">上传文件名</param>
        /// <param name="folderId">父目录ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<ApiStatus> UploadGroupFile(long groupId, string localFilePath, string fileName,
                                                          string folderId = null)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.UploadGroupFile(ConnectionId, groupId, localFilePath,
                                                      fileName, folderId);
        }

        /// <summary>
        /// 设置精华消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<ApiStatus> SetEssenceMessage(long messageId)
        {
            return await ApiInterface.SetEssenceMsg(ConnectionId, messageId);
        }

        /// <summary>
        /// 删除精华消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<ApiStatus> DelEssenceMessage(long messageId)
        {
            return await ApiInterface.DelEssenceMsg(ConnectionId, messageId);
        }

        /// <summary>
        /// 获取群精华消息列表
        /// </summary>
        /// <param name="gid">群号</param>
        /// <returns>精华消息列表</returns>
        public async ValueTask<(ApiStatus apiStatus, List<EssenceInfo> essenceInfos)> GetEssenceMsgList(long gid)
        {
            return await ApiInterface.GetEssenceMsgList(ServiceId, ConnectionId, gid);
        }

        /// <summary>
        /// <para>使用腾讯API检查链接安全性</para>
        /// <para>腾讯的这东西感觉不靠谱（</para>
        /// </summary>
        /// <param name="url">需要检查的链接</param>
        /// <returns>安全性</returns>
        public async ValueTask<(ApiStatus apiStatus, SecurityLevelType securityLevel)> CheckUrlSafely(string url)
        {
            return await ApiInterface.CheckUrlSafely(ConnectionId, url);
        }

        /// <summary>
        /// 发送群公告
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="content">公告内容</param>
        /// <param name="image">图片</param>
        public async ValueTask<ApiStatus> SendGroupNotice(long groupId, string content, string image = null)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.SendGroupNotice(ConnectionId, groupId, content, image);
        }

        /// <summary>
        /// 在群根目录创建文件夹
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="name">文件夹名</param>
        public async ValueTask<ApiStatus> CreateGroupFileRootFolder(long groupId, string name)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.CreateGroupFileFolder(ConnectionId, groupId, name, null);
        }

        /// <summary>
        /// 删除群文件文件夹
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="folderId">文件夹ID</param>
        public async ValueTask<ApiStatus> DeleteGroupFolder(long groupId, string folderId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.DeleteGroupFolder(ConnectionId, groupId, folderId);
        }

        /// <summary>
        /// 删除群文件文件夹
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <para>文件夹ID</para>
        /// <para>在删除根目录文件时置空</para>
        /// <param name="fileId">文件ID</param>
        /// <param name="busId">文件类型</param>
        public async ValueTask<ApiStatus> DeleteGroupFile(long groupId, string fileId, int busId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return await ApiInterface.DeleteGroupFile(ConnectionId, groupId, fileId, busId);
        }

        #endregion

        #endregion

        #region 账号API

        /// <summary>
        /// <para>获取登陆QQ的名字</para>
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="nick"/> 账号昵称</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, long uid, string nick)> GetLoginInfo()
        {
            return await ApiInterface.GetLoginInfo(ConnectionId);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="friendList"/> 好友列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<FriendInfo> friendList)> GetFriendList()
        {
            return await ApiInterface.GetFriendList(ServiceId, ConnectionId);
        }

        /// <summary>
        /// 获取群组列表
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="groupList"/> 群组列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<GroupInfo> groupList)> GetGroupList(bool useCache = true)
        {
            return await ApiInterface.GetGroupList(ConnectionId, useCache);
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 群成员列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList(
            long groupId, bool useCache = true)
        {
            if (groupId < 100000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} out of range");
            return await ApiInterface.GetGroupMemberList(ServiceId, ConnectionId, groupId, useCache);
        }

        /// <summary>
        /// 获取群信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="GroupInfo"/> 群信息列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, GroupInfo groupInfo)> GetGroupInfo(
            long groupId, bool useCache = true)
        {
            if (groupId < 100000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} out of range");
            return await ApiInterface.GetGroupInfo(ConnectionId, groupId, useCache);
        }

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="GroupMemberInfo"/> 群成员信息</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
            long groupId, long userId, bool useCache = true)
        {
            if (groupId < 100000 && userId < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return await ApiInterface.GetGroupMemberInfo(ServiceId, ConnectionId, groupId, userId, useCache);
        }

        /// <summary>
        /// <para>获取用户信息</para>
        /// <para>注意此API获取的权限等级不是群内权限等级</para>
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache">使用缓存</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="UserInfo"/> 群成员信息</para>
        /// <para><see cref="string"/> qid</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, UserInfo userInfo, string qid)> GetUserInfo(
            long userId, bool useCache = true)
        {
            if (userId < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            return await ApiInterface.GetUserInfo(ServiceId, ConnectionId, userId, useCache);
        }

        /// <summary>
        /// <para>获取vip信息[不能获取非好友]</para>
        /// <para>注意:此为不稳定API</para>
        /// </summary>
        /// <param name="userId">用户ID</param>
        [Obsolete]
        public async ValueTask<(ApiStatus apiStatus, VipInfo vipInfo)> GetVipInfo(long userId)
        {
            if (userId < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            return await ApiInterface.GetVipInfo(ConnectionId, userId);
        }

        /// <summary>
        /// <para>获取企点账号信息</para>
        /// <para>该API只有企点协议可用</para>
        /// </summary>
        public async ValueTask<(ApiStatus apiStatus, QidianAccountInfo qidianAccountInfo)> GetQidianAccountInfo()
        {
            return await ApiInterface.GetQidianAccountInfo(ConnectionId);
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// </returns>
        public async ValueTask<ApiStatus> DeleteFriend(long userId)
        {
            return await ApiInterface.DeleteFriend(ConnectionId, userId);
        }

        /// <summary>
        /// 获取在线机型
        /// </summary>
        /// <param name="model">型号</param>
        public async ValueTask<(ApiStatus apiStatus, List<Model> models)> GetModelShow(string model)
        {
            return await ApiInterface.GetModelShow(ConnectionId, model);
        }

        /// <summary>
        /// 设置在线机型
        /// </summary>
        /// <param name="model">机型名</param>
        /// <param name="showModel">展示名</param>
        public async ValueTask<ApiStatus> SetModelShow(string model, string showModel)
        {
            return await ApiInterface.SetModelShow(ConnectionId, model, showModel);
        }

        /// <summary>
        /// 获取单向好友列表
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 单向好友列表</para>
        /// <para>T = <see cref="UnidirectionalFriendInfo"/> 单向好友信息</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<UnidirectionalFriendInfo> unidirectionalFriendInfos)>
            GetUnidirectionalFriendList()
        {
            return await ApiInterface.GetUnidirectionalFriendList(ConnectionId);
        }

        /// <summary>
        /// 删除单向好友
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async ValueTask<ApiStatus> DeleteUnidirectionalFriend(long userId)
        {
            return await ApiInterface.DeleteUnidirectionalFriend(ConnectionId, userId);
        }

        #endregion

        #region 服务端API

        /// <summary>
        /// 获取连接客户端版本信息
        /// </summary>
        /// <returns>
        /// <para>客户端类型</para>
        /// <para><see langword="ver"/>客户端版本号</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, string clientType, string ver)> GetClientInfo()
        {
            return await ApiInterface.GetClientInfo(ConnectionId);
        }

        /// <summary>
        /// 检查是否可以发送图片
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendImage()
        {
            return await ApiInterface.CanSendImage(ConnectionId);
        }

        /// <summary>
        /// 检查是否可以发送语音
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, bool canSend)> CanSendRecord()
        {
            return await ApiInterface.CanSendRecord(ConnectionId);
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="online"/> 客户端是否在线</para>
        /// <para><see langword="good"/> 客户端是否正常运行</para>
        /// <para><see langword="statData"/> 统计信息，如为go-cqhttp详细内容参照文档：https://ishkong.github.io/go-cqhttp-docs/api/#%E8%8E%B7%E5%8F%96%E7%8A%B6%E6%80%81</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, bool online, bool good, JObject )> GetStatus()
        {
            return await ApiInterface.GetStatus(ConnectionId);
        }

        /// <summary>
        /// 重启客户端
        /// </summary>
        /// <param name="delay">延迟(ms)</param>
        public async ValueTask<ApiStatus> RebootClient(int delay = 0)
        {
            if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            return await ApiInterface.Restart(ConnectionId, delay);
        }

        /// <summary>
        /// 重载事件过滤器
        /// </summary>
        public async ValueTask<ApiStatus> ReloadEventFilter()
        {
            return await ApiInterface.ReloadEventFilter(ConnectionId);
        }

        #endregion

        #region 请求处理API

        /// <summary>
        /// 处理加好友请求
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="approve">是否同意</param>
        /// <param name="remark">好友备注</param>
        public async ValueTask<ApiStatus> SetFriendAddRequest(string flag, bool approve,
                                                              string remark = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            return await ApiInterface.SetFriendAddRequest(ConnectionId, flag, approve, remark);
        }

        /// <summary>
        /// 处理加群请求/邀请
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="approve">是否同意</param>
        /// <param name="reason">拒绝理由</param>
        public async ValueTask<ApiStatus> SetGroupAddRequest(string flag,
                                                             GroupRequestType requestType,
                                                             bool approve,
                                                             string reason = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            return await ApiInterface.SetGroupAddRequest(ConnectionId, flag, requestType,
                                                         approve, reason);
        }

        #endregion

        #region 辅助API

        #region GoAPI

        /// <summary>
        /// 获取中文分词
        /// </summary>
        /// <param name="text">内容</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="wordList"/> 分词列表</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<string> wordList)> GetWordSlices(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new NullReferenceException(nameof(text));
            return await ApiInterface.GetWordSlices(ConnectionId, text);
        }

        /// <summary>
        /// <para>下载文件到缓存目录</para>
        /// <para>注意：此API的调用超时时间是独立于其他API的</para>
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="threadCount">下载线程数</param>
        /// <param name="customHeader">自定义请求头</param>
        /// <param name="timeout">超时(ms)</param>
        /// <returns>文件绝对路径</returns>
        public async ValueTask<(ApiStatus apiStatus, string filePath)> DownloadFile(
            string url, int threadCount, Dictionary<string, string> customHeader = null,
            int timeout = 10000)
        {
            if (string.IsNullOrEmpty(url)) throw new NullReferenceException(nameof(url));
            if (threadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "threadCount less than 1");
            if (timeout < 1000)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout less than 1000");
            return await ApiInterface.DownloadFile(url, threadCount, ConnectionId, customHeader, timeout);
        }

        /// <summary>
        /// OCR图片
        /// </summary>
        /// <param name="imageId">图片ID</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="texts"/> 识别结果</para>
        /// <para><see langword="language"/> 识别语言</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, List<TextDetection> texts, string lang)> OcrImage(string imageId)
        {
            return await ApiInterface.OcrImage(imageId, ConnectionId);
        }

        #endregion

        #endregion

        #endregion

        #region 框架类API

        /// <summary>
        /// 获取用户实例
        /// </summary>
        /// <param name="userId">用户id</param>
        public User GetUser(long userId)
        {
            if (userId < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            return new User(ServiceId, ConnectionId, userId);
        }

        /// <summary>
        /// 获取群实例
        /// </summary>
        /// <param name="groupId">群id</param>
        public Group GetGroup(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return new Group(ServiceId, ConnectionId, groupId);
        }

        /// <summary>
        /// <para>获取登录账号的id</para>
        /// <para>使用正向ws链接时此方法在触发lifecycle事件前失效</para>
        /// </summary>
        public long GetLoginUserId()
        {
            if (ConnectionManager.GetLoginUid(ConnectionId, out var uid)) return uid;
            return -1;
        }

        /// <summary>
        /// 屏蔽用户
        /// 在当前服务实例内不再处理其消息
        /// </summary>
        /// <param name="userId">用户ID</param>
        public bool BlockUser(long userId)
            => StaticVariable.ServiceInfos[ServiceId].BlockUsers.Add(userId);

        /// <summary>
        /// 对用户解除屏蔽
        /// </summary>
        /// <param name="userId">用户ID</param>
        public bool RemoveBlock(long userId)
            => StaticVariable.ServiceInfos[ServiceId].BlockUsers.Remove(userId);

        /// <summary>
        /// 添加机器人管理员
        /// </summary>
        /// <param name="userId">用户ID</param>
        public bool AddSuperUser(long userId)
            => StaticVariable.ServiceInfos[ServiceId].SuperUsers.Add(userId);

        /// <summary>
        /// 解除机器人管理员
        /// </summary>
        /// <param name="userId">用户ID</param>
        public bool RemoveSuperUser(long userId)
            => StaticVariable.ServiceInfos[ServiceId].SuperUsers.Remove(userId);

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(SoraApi apiL, SoraApi apiR)
        {
            if (apiL is null && apiR is null) return true;

            return apiL is not null && apiR is not null && apiL.ConnectionId == apiR.ConnectionId;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(SoraApi apiL, SoraApi apiR)
        {
            return !(apiL == apiR);
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SoraApi api)
            {
                return this == api;
            }

            return false;
        }

        /// <summary>
        /// GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            return ConnectionId.GetHashCode();
        }

        #endregion
    }
}