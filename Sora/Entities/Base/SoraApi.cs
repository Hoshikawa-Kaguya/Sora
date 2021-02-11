using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Server;
using Sora.Server.ServerInterface;

namespace Sora.Entities.Base
{
    /// <summary>
    /// Sora API类
    /// </summary>
    public sealed class SoraApi
    {
        #region 属性
        /// <summary>
        /// 当前实例对应的链接GUID
        /// 用于调用API
        /// </summary>
        private Guid ConnectionGuid { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化Api实例
        /// </summary>
        /// <param name="connectionGuid"></param>
        internal SoraApi(Guid connectionGuid)
        {
            this.ConnectionGuid = connectionGuid;
        }
        #endregion

        #region 通讯类API

        #region 消息API
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="userId">发送目标用户id</param>
        /// <param name="message">消息</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendPrivateMessage(long userId, params object[] message)
        {
            if(userId         < 10000) throw new ArgumentOutOfRangeException($"{nameof(userId)} too small");
            if(message.Length == 0) throw new NullReferenceException(nameof(message));
            //消息段列表
            List<CQCode> msgList = new List<CQCode>();
            foreach (object msgObj in message)
            {
                if(msgObj is CQCode cqCode)
                {
                    msgList.Add(cqCode);
                }
                else if (msgObj is IEnumerable<CQCode> cqCodes)
                {
                    msgList.AddRange(cqCodes);
                }
                else
                {
                    msgList.Add(CQCode.CQText(msgObj.ToString()));
                }
            }
            return ((APIStatusType apiStatus, int messageId)) await ApiInterface.SendPrivateMessage(this.ConnectionGuid, userId, msgList);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="userId">发送目标群id</param>
        /// <param name="message">
        /// <para>消息</para>
        /// <para><see cref="List{T}"/>(T = <see cref="CQCode"/>)</para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendPrivateMessage(long userId, List<CQCode> message)
        {
            if(userId        < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            if(message.Count == 0) throw new NullReferenceException(nameof(message));
            return ((APIStatusType apiStatus, int messageId))
                await ApiInterface.SendPrivateMessage(this.ConnectionGuid, userId, message);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="groupId">发送目标群id</param>
        /// <param name="message">
        /// <para>消息</para>
        /// <para>可以为<see cref="string"/>/<see cref="CQCode"/>)</para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(long groupId, params object[] message)
        {
            if(groupId        < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if(message.Length == 0) throw new NullReferenceException(nameof(message));
            //消息段列表
            List<CQCode> msgList = new List<CQCode>();
            foreach (object msgObj in message)
            {
                if (msgObj is IEnumerable<CQCode> cqCodeList)
                {
                    msgList.AddRange(cqCodeList);
                }
                else if(msgObj is CQCode cqCode)
                {
                    msgList.Add(cqCode);
                }
                else
                {
                    msgList.Add(CQCode.CQText(msgObj.ToString()));
                }
            }
            return ((APIStatusType apiStatus, int messageId))
                await ApiInterface.SendGroupMessage(this.ConnectionGuid, groupId, msgList);
        }

        /// <summary>
        /// 发送群聊消息
        /// </summary>
        /// <param name="groupId">发送目标群id</param>
        /// <param name="message">
        /// <para>消息</para>
        /// <para><see cref="List{T}"/>(T = <see cref="CQCode"/>)</para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(long groupId, List<CQCode> message)
        {
            if(groupId        < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if(message.Count == 0) throw new NullReferenceException(nameof(message));
            return ((APIStatusType apiStatus, int messageId))
                await ApiInterface.SendGroupMessage(this.ConnectionGuid, groupId, message);
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        public async ValueTask RecallMessage(int messageId)
        {
            await ApiInterface.RecallMsg(this.ConnectionGuid, messageId);
        }

        #region GoAPI
        /// <summary>
        /// 获取合并转发消息
        /// </summary>
        /// <param name="forwardId"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="nodeArray"/> 消息节点列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<Node> nodeArray)> GetForwardMessage(string forwardId)
        {
            if(string.IsNullOrEmpty(forwardId)) throw new NullReferenceException(nameof(forwardId));
            var (retCode, nodeArray) = await ApiInterface.GetForwardMessage(this.ConnectionGuid, forwardId);
            return ((APIStatusType) retCode, nodeArray.NodeMsgList);
        }

        /// <summary>
        /// 发送合并转发(群)
        /// 但好像不能用的样子
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="nodeList">
        /// 节点(<see cref="Node"/>)消息段列表
        /// </param>
        public async ValueTask SendGroupForwardMsg(long groupId, List<CustomNode> nodeList)
        {
            if(groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if(nodeList == null || nodeList.Count == 0) throw new NullReferenceException(nameof(nodeList));
            await ApiInterface.SendGroupForwardMsg(this.ConnectionGuid, groupId, nodeList);
        }

        /// <summary>
        /// 获取图片信息
        /// </summary>
        /// <param name="cacheFileName">缓存文件名</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="size"/> 文件大小(Byte)</para>
        /// <para><see langword="fileName"/> 文件名</para>
        /// <para><see langword="url"/> 文件链接</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int size, string fileName, string url)> GetImage(
            string cacheFileName)
        {
            if(string.IsNullOrEmpty(cacheFileName)) throw new ArgumentOutOfRangeException(nameof(cacheFileName));
            return ((APIStatusType apiStatus, int size, string fileName, string url))
                await ApiInterface.GetImage(this.ConnectionGuid, cacheFileName);
        }

        /// <summary>
        /// <para>获取群消息</para>
        /// <para>只能获取纯文本信息</para>
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="Message"/> 消息内容</para>
        /// <para><see cref="User"/> 发送者</para>
        /// <para><see cref="Group"/> 消息来源群，如果不是群消息则为<see langword="null"/></para>
        /// </returns>
        public async
            ValueTask<(APIStatusType apiStatus, Message message, User sender, Group sourceGroup,
                int realId, bool isGroupMsg)> GetMessages(int messageId)
        {
            return ((APIStatusType apiStatus, Message message, User sender, Group sourceGroup, int realId, bool isGroupMsg)) 
                await ApiInterface.GetMessage(this.ConnectionGuid, messageId);
        }

        /// <summary>
        /// <para>获取群消息历史记录</para>
        /// <para>能获取起始消息的前19条消息</para>
        /// </summary>
        /// <param name="messageSequence">起始消息序号，为<see langword="null"/>时默认从最新消息拉取</param>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 消息记录</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupMessageEventArgs> messages)> GetGroupMessageHistory(long groupId, int? messageSequence = null)
        {
            return ((APIStatusType apiStatus, List<GroupMessageEventArgs> messages))
                await ApiInterface.GetGroupMessageHistory(messageSequence, groupId, this.ConnectionGuid);
        }

        /// <summary>
        /// 获取当前账号在线客户端列表
        /// </summary>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 在线客户端信息列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<ClientInfo> clients)> GetOnlineClients(bool useCache)
        {
            return ((APIStatusType apiStatus, List<ClientInfo> clients))
                await ApiInterface.GetOnlineClients(useCache, this.ConnectionGuid);
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
        public async ValueTask SetGroupCard(long groupId, long userId, string card)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupCard(this.ConnectionGuid, groupId, userId, card);
        }

        /// <summary>
        /// 设置群成员专属头衔
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="specialTitle">专属头衔(为空时清空)</param>
        public async ValueTask SetGroupMemberSpecialTitle(long groupId, long userId, string specialTitle)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupSpecialTitle(this.ConnectionGuid, groupId, userId, specialTitle);
        }

        /// <summary>
        /// 群组踢人
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="rejectRequest">拒绝此人的加群请求</param>
        public async ValueTask KickGroupMember(long groupId, long userId, bool rejectRequest)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupKick(this.ConnectionGuid, groupId, userId, rejectRequest);
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
        public async ValueTask EnableGroupMemberMute(long groupId, long userId, long duration)
        {
            if (groupId is < 100000 || userId is < 10000 || duration < 60)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} or {nameof(duration)} out of range");
            await ApiInterface.SetGroupBan(this.ConnectionGuid, groupId, userId, duration);
        }

        /// <summary>
        /// 解除群组成员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        public async ValueTask DisableGroupMemberMute(long groupId, long userId)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupBan(this.ConnectionGuid, groupId, userId, 0);
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="anonymous">匿名用户对象</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        public async ValueTask EnableGroupAnonymousMute(long groupId, Anonymous anonymous, long duration)
        {
            if (groupId is < 100000 || duration < 60)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(duration)} out of range");
            if(anonymous == null) 
                throw new NullReferenceException("anonymous is null");
            await ApiInterface.SetAnonymousBan(this.ConnectionGuid, groupId, anonymous, duration);
        }

        /// <summary>
        /// 群组匿名用户禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="anonymousFlag">匿名用户Flag</param>
        /// <param name="duration">禁言时长, 单位秒</param>
        public async ValueTask EnableGroupAnonymousMute(long groupId, string anonymousFlag, long duration)
        {
            if (groupId is < 100000 || duration < 60)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(duration)} out of range");
            if(anonymousFlag == null) 
                throw new NullReferenceException("anonymousFlag is null");
            await ApiInterface.SetAnonymousBan(this.ConnectionGuid, groupId, anonymousFlag, duration);
        }

        /// <summary>
        /// 群组全员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask EnableGroupMute(long groupId)
        {
            if(groupId < 100000)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            await ApiInterface.SetGroupWholeBan(this.ConnectionGuid, groupId, true);
        }

        /// <summary>
        /// 解除群组全员禁言
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask DisableGroupMute(long groupId)
        {
            if(groupId < 100000)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            await ApiInterface.SetGroupWholeBan(this.ConnectionGuid, groupId, false);
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask EnableGroupAdmin(long groupId, long userId)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupAdmin(this.ConnectionGuid, userId, groupId, true);
        }

        /// <summary>
        /// 取消群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask DisableGroupAdmin(long groupId, long userId)
        {
            if (groupId is < 100000 || userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            await ApiInterface.SetGroupAdmin(this.ConnectionGuid, userId, groupId, false);
        }

        /// <summary>
        /// 退出群
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask LeaveGroup(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            await ApiInterface.SetGroupLeave(this.ConnectionGuid, groupId, false);
        }

        /// <summary>
        /// 解散群
        /// </summary>
        /// <param name="groupId">群号</param>
        public async ValueTask DismissGroup(long groupId)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            await ApiInterface.SetGroupLeave(this.ConnectionGuid, groupId, true);
        }

        #region GoAPI
        /// <summary>
        /// 设置群名
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="newName">新群名</param>
        public async ValueTask SetGroupName(long groupId, string newName)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (string.IsNullOrEmpty(newName)) throw new NullReferenceException(nameof(newName));
            await ApiInterface.SetGroupName(this.ConnectionGuid, groupId, newName);
        }

        /// <summary>
        /// 设置群头像
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="imageFile">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用缓存</param>
        public async ValueTask SetGroupPortrait(long groupId, string imageFile, bool useCache = true)
        {
            if (groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            if (string.IsNullOrEmpty(imageFile)) throw new NullReferenceException(nameof(imageFile));
            (string retFileStr, bool isMatch) = CQCode.ParseDataStr(imageFile);
            if (!isMatch) throw new NotSupportedException($"not supported file type({imageFile})");
            await ApiInterface.SetGroupPortrait(this.ConnectionGuid, groupId, retFileStr, useCache);
        }

        /// <summary>
        /// 获取群组系统消息
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="joinList"/> 进群消息列表</para>
        /// <para><see langword="invitedList"/> 邀请消息列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupRequestInfo> joinList, List<GroupRequestInfo> invitedList)>
            GetGroupSystemMsg()
        {
            return ((APIStatusType apiStatus, List<GroupRequestInfo> joinList, List<GroupRequestInfo> invitedList))
                await ApiInterface.GetGroupSystemMsg(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupFileSysInfo"/> 文件系统信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupFileSysInfo groupFileSysInfo)> GetGroupFileSysInfo(
            long groupId)
        {
            return ((APIStatusType apiStatus, GroupFileSysInfo groupFileSysInfo))
                await ApiInterface.GetGroupFileSysInfo(groupId, this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupRootFiles(long groupId)
        {
            return ((APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders))
                await ApiInterface.GetGroupRootFiles(groupId, this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="foldId">文件夹ID</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupFilesByFolder(long groupId, string foldId)
        {
            return ((APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders))
                await ApiInterface.GetGroupFilesByFolder(groupId, foldId, this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群@全体成员剩余次数
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canAt"/> 是否可以@全体成员</para>
        /// <para><see langword="groupRemain"/> 群内所有管理当天剩余@全体成员次数</para>
        /// <para><see langword="botRemain"/> BOT当天剩余@全体成员次数</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canAt, short groupRemain, short botRemain)>
            GetGroupAtAllRemain(long groupId)
        {
            return ((APIStatusType apiStatus, bool canAt, short groupRemain, short botRemain)) await ApiInterface
                .GetGroupAtAllRemain(groupId, this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群文件资源链接
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="fileId">文件ID</param>
        /// <param name="busid">文件类型</param>
        /// <returns>文件链接</returns>
        public async ValueTask<(APIStatusType apiStatus, string fileUrl)> GetGroupFileUrl(
            long groupId, string fileId, int busid)
        {
            return ((APIStatusType apiStatus, string fileUrl))
                await ApiInterface.GetGroupFileUrl(groupId, fileId, busid, this.ConnectionGuid);
        }

        /// <summary>
        /// 上传群文件
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="fileName">上传文件名</param>
        /// <param name="floderId">父目录ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<APIStatusType> UploadGroupFile(long groupId, string localFilePath, string fileName,
                                                              string floderId = null)
        {
            return (APIStatusType) await ApiInterface.UploadGroupFile(this.ConnectionGuid, groupId, localFilePath, fileName, floderId);
        }

        /// <summary>
        /// 设置精华消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<APIStatusType> SetEssenceMessage(long messageId)
        {
            return (APIStatusType) await ApiInterface.SetEssenceMsg(this.ConnectionGuid, messageId);
        }

        /// <summary>
        /// 删除精华消息
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<APIStatusType> DelEssenceMessage(long messageId)
        {
            return (APIStatusType) await ApiInterface.DelEssenceMsg(this.ConnectionGuid, messageId);
        }

        /// <summary>
        /// 获取群精华消息列表
        /// </summary>
        /// <param name="gid">群号</param>
        /// <returns>精华消息列表</returns>
        public async ValueTask<(APIStatusType apiStatus, List<EssenceInfo> essenceInfos)> GetEssenceMsgList(long gid)
        {
            return ((APIStatusType apiStatus, List<EssenceInfo> essenceInfos))
                await ApiInterface.GetEssenceMsgList(this.ConnectionGuid, gid);
        }

        /// <summary>
        /// <para>使用腾讯API检查链接安全性</para>
        /// <para>腾讯的这东西感觉不靠谱（</para>
        /// </summary>
        /// <param name="url">需要检查的链接</param>
        /// <returns>安全性</returns>
        public async ValueTask<(APIStatusType apiStatus, SecurityLevelType securityLevel)> CheckUrlSafely(string url)
        {
            return ((APIStatusType apiStatus, SecurityLevelType securityLevel))
                await ApiInterface.CheckUrlSafely(this.ConnectionGuid, url);
        }
        #endregion
        #endregion

        #region 账号信息API
        /// <summary>
        /// <para>获取登陆QQ的名字</para>
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="nick"/> 账号昵称</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, string nick)> GetLoginUserName()
        {
            (int retCode,_,string nick) = await ApiInterface.GetLoginInfo(this.ConnectionGuid);
            return ((APIStatusType)retCode, nick);
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="friendList"/> 好友列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<FriendInfo> friendList)> GetFriendList()
        {
            return ((APIStatusType apiStatus, List<FriendInfo> friendList)) 
                await ApiInterface.GetFriendList(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群组列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupList"/> 群组列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupInfo> groupList)> GetGroupList()
        {
            return ((APIStatusType apiStatus, List<GroupInfo> groupList)) 
                await ApiInterface.GetGroupList(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 群成员列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList(long groupId)
        {
            if (groupId < 100000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} out of range");
            return ((APIStatusType apiStatus, List<GroupMemberInfo> groupMemberList)) 
                await ApiInterface.GetGroupMemberList(this.ConnectionGuid, groupId);
        }

        /// <summary>
        /// 获取群信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupInfo"/> 群信息列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupInfo groupInfo)> GetGroupInfo(long groupId, bool useCache = true)
        {
            if (groupId is < 100000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} out of range");
            return ((APIStatusType apiStatus, GroupInfo groupInfo)) 
                await ApiInterface.GetGroupInfo(this.ConnectionGuid, groupId, useCache);
        }

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupMemberInfo"/> 群成员信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
            long groupId, long userId, bool useCache = true)
        {
            if (groupId is < 100000 && userId is < 10000)
                throw new ArgumentOutOfRangeException($"{nameof(groupId)} or {nameof(userId)} out of range");
            return ((APIStatusType apiStatus, GroupMemberInfo memberInfo)) 
                await ApiInterface.GetGroupMemberInfo(this.ConnectionGuid, groupId, userId, useCache);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache"></param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="UserInfo"/> 群成员信息</para>
        /// <para><see cref="string"/> qid</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, UserInfo userInfo, string qid)> GetUserInfo(
            long userId, bool useCache = true)
        {
            if(userId < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            return ((APIStatusType apiStatus, UserInfo userInfo, string qid))
                await ApiInterface.GetUserInfo(this.ConnectionGuid, userId, useCache);
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
        public async ValueTask<(APIStatusType apiStatus, string clientType, string ver)> GetClientInfo()
        {
            return ((APIStatusType apiStatus, string clientType, string ver))
                await ApiInterface.GetClientInfo(this.ConnectionGuid);
        }

        /// <summary>
        /// 检查是否可以发送图片
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canSend)> CanSendImage()
        {
            return ((APIStatusType apiStatus, bool canSend)) 
                await ApiInterface.CanSendImage(this.ConnectionGuid);
        }

        /// <summary>
        /// 检查是否可以发送语音
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canSend"/> 是否能发送</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canSend)> CanSendRecord()
        {
            return ((APIStatusType apiStatus, bool canSend)) 
                await ApiInterface.CanSendRecord(this.ConnectionGuid);
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="online"/> 客户端是否在线</para>
        /// <para><see langword="good"/> 客户端是否正常运行</para>
        /// <para><see langword="statData"/> 统计信息，如为go-cqhttp详细内容参照文档：https://ishkong.github.io/go-cqhttp-docs/api/#%E8%8E%B7%E5%8F%96%E7%8A%B6%E6%80%81</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool online, bool good, JObject )> GetStatus()
        {
            return ((APIStatusType apiStatus, bool online, bool good, JObject statData))
                await ApiInterface.GetStatus(this.ConnectionGuid);
        }

        /// <summary>
        /// 重启客户端
        /// </summary>
        /// <param name="delay">延迟(ms)</param>
        public async ValueTask RebootClient(int delay = 0)
        {
            if(delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
            await ApiInterface.Restart(this.ConnectionGuid, delay);
        }

        /// <summary>
        /// 重载事件过滤器
        /// </summary>
        public async ValueTask ReloadEventFilter()
        {
            await ApiInterface.ReloadEventFilter(this.ConnectionGuid);
        }
        #endregion

        #region 请求处理API
        /// <summary>
        /// 处理加好友请求
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="approve">是否同意</param>
        /// <param name="remark">好友备注</param>
        public async ValueTask SetFriendAddRequest(string flag, bool approve,
                                                   string remark = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            await ApiInterface.SetFriendAddRequest(this.ConnectionGuid, flag, approve, remark);
        }

        /// <summary>
        /// 处理加群请求/邀请
        /// </summary>
        /// <param name="flag">请求flag</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="approve">是否同意</param>
        /// <param name="reason">拒绝理由</param>
        public async ValueTask SetGroupAddRequest(string flag,
                                                  GroupRequestType requestType,
                                                  bool approve,
                                                  string reason = null)
        {
            if (string.IsNullOrEmpty(flag)) throw new NullReferenceException(nameof(flag));
            await ApiInterface.SetGroupAddRequest(this.ConnectionGuid, flag, requestType, approve, reason);
        }
        #endregion

        #region 辅助API

        #region GoAPI
        /// <summary>
        /// 获取中文分词
        /// </summary>
        /// <param name="text">内容</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="wordList"/> 分词列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<string> wordList)> GetWordSlices(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new NullReferenceException(nameof(text));
            return ((APIStatusType apiStatus, List<string> wordList)) await ApiInterface.GetWordSlices(this.ConnectionGuid, text);
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
        public async ValueTask<(APIStatusType retCode, string filePath)> DownloadFile(
            string url, int threadCount, Dictionary<string, string> customHeader = null,
            int timeout = 10000)
        {
            if (string.IsNullOrEmpty(url)) throw new NullReferenceException(nameof(url));
            if (threadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "threadCount is less than 1");
            if (timeout < 1000)
                throw new ArgumentOutOfRangeException(nameof(timeout), "timeout is less than 1000");
            return ((APIStatusType retCode, string filePath))
                await ApiInterface.DownloadFile(url, threadCount, this.ConnectionGuid, customHeader, timeout);
        }

        /// <summary>
        /// OCR图片
        /// </summary>
        /// <param name="imageId">图片ID</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="texts"/> 识别结果</para>
        /// <para><see langword="language"/> 识别语言</para>
        /// </returns>
        public async ValueTask<(APIStatusType retCode, List<TextDetection> texts, string lang)> OcrImage(string imageId)
        {
            return ((APIStatusType retCode, List<TextDetection> texts, string lang))
                await ApiInterface.OcrImage(imageId, this.ConnectionGuid);
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
            if(userId < 10000) throw new ArgumentOutOfRangeException(nameof(userId));
            return new User(this.ConnectionGuid, userId);
        }

        /// <summary>
        /// 获取群实例
        /// </summary>
        /// <param name="groupId">群id</param>
        public Group GetGroup(long groupId)
        {
            if(groupId < 100000) throw new ArgumentOutOfRangeException(nameof(groupId));
            return new Group(this.ConnectionGuid, groupId);
        }

        /// <summary>
        /// 获取登录账号的id
        /// </summary>
        public long GetLoginUserId()
        {
            if (ConnectionManager.GetLoginUid(this.ConnectionGuid, out long uid))
            {
                return uid;
            }
            return -1;
        }
        #endregion
    }
}
