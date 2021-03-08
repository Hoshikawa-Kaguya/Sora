using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;

namespace Sora.Entities
{
    /// <summary>
    /// 群组类
    /// </summary>
    public sealed class Group : BaseModel
    {
        #region 属性

        /// <summary>
        /// 群号
        /// </summary>
        public long Id { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器连接标识</param>
        /// <param name="gid">群号</param>
        internal Group(Guid connectionGuid, long gid) : base(connectionGuid)
        {
            this.Id = gid;
        }

        #endregion

        #region 群消息类方法

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="message">
        /// <para>消息</para>
        /// <para>可以为<see cref="string"/>/<see cref="CQCode"/></para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(params object[] message)
        {
            return await base.SoraApi.SendGroupMessage(this.Id, message);
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="message">
        /// <para>消息</para>
        /// <para><see cref="List{T}"/>(T = <see cref="CQCode"/>)</para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(List<CQCode> message)
        {
            return await base.SoraApi.SendGroupMessage(this.Id, message);
        }

        #endregion

        #region 群信息类方法

        /// <summary>
        /// 获取群信息
        /// </summary>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupInfo"/> 群信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupInfo groupInfo)> GetGroupInfo(bool useCache = true)
        {
            return await base.SoraApi.GetGroupInfo(this.Id, useCache);
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="List{GroupMemberInfo}"/> 群成员列表</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupMemberInfo> groupMemberList)> GetGroupMemberList()
        {
            return await base.SoraApi.GetGroupMemberList(this.Id);
        }

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupMemberInfo"/> 群成员信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupMemberInfo memberInfo)> GetGroupMemberInfo(
            long userId, bool useCache = true)
        {
            return await base.SoraApi.GetGroupMemberInfo(this.Id, userId, useCache);
        }

        #region Go扩展

        /// <summary>
        /// 发送合并转发(群)
        /// 但好像不能用的样子
        /// </summary>
        /// <param name="nodeList">
        /// 节点(<see cref="Node"/>)消息段列表
        /// </param>
        public async ValueTask<APIStatusType> SendGroupForwardMsg(IEnumerable<CustomNode> nodeList)
        {
            return await base.SoraApi.SendGroupForwardMsg(this.Id, nodeList);
        }

        /// <summary>
        /// 获取群文件系统信息
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="GroupFileSysInfo"/> 文件系统信息</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, GroupFileSysInfo groupFileSysInfo)> GetGroupFileSysInfo()
        {
            return await SoraApi.GetGroupFileSysInfo(Id);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupRootFiles()
        {
            return await SoraApi.GetGroupRootFiles(Id);
        }

        /// <summary>
        /// 获取群根目录文件列表
        /// </summary>
        /// <param name="foldId">文件夹ID</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="groupFiles"/> 文件列表</para>
        /// <para><see langword="groupFolders"/> 文件夹列表</para>
        /// </returns>
        public async
            ValueTask<(APIStatusType apiStatus, List<GroupFileInfo> groupFiles, List<GroupFolderInfo> groupFolders)>
            GetGroupFilesByFolder(string foldId)
        {
            return await SoraApi.GetGroupFilesByFolder(Id, foldId);
        }

        /// <summary>
        /// 获取群文件资源链接
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="busid">文件类型</param>
        /// <returns>文件链接</returns>
        public async ValueTask<(APIStatusType apiStatus, string fileUrl)> GetGroupFileUrl(
            string fileId, int busid)
        {
            return await SoraApi.GetGroupFileUrl(Id, fileId, busid);
        }

        /// <summary>
        /// <para>获取群消息历史记录</para>
        /// <para>能获取起始消息的前19条消息</para>
        /// </summary>
        /// <param name="messageSequence">起始消息序号，为<see langword="null"/>时默认从最新消息拉取</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see cref="List{T}"/> 消息记录</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, List<GroupMessageEventArgs> messages)> GetGroupMessageHistory(
            int? messageSequence = null)
        {
            return await base.SoraApi.GetGroupMessageHistory(this.Id, messageSequence);
        }

        /// <summary>
        /// 发送群公告
        /// </summary>
        /// <param name="content">公告内容</param>
        public async ValueTask<APIStatusType> SendGroupNotice(string content)
        {
            return await base.SoraApi.SendGroupNotice(this.Id, content);
        }

        /// <summary>
        /// 获取群精华消息列表
        /// </summary>
        /// <returns>精华消息列表</returns>
        public async ValueTask<(APIStatusType apiStatus, List<EssenceInfo> essenceInfos)> GetEssenceMsgList()
        {
            return await base.SoraApi.GetEssenceMsgList(this.Id);
        }

        /// <summary>
        /// 上传群文件
        /// </summary>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="fileName">上传文件名</param>
        /// <param name="floderId">父目录ID</param>
        /// <returns>API状态</returns>
        public async ValueTask<APIStatusType> UploadGroupFile(string localFilePath, string fileName,
                                                              string floderId = null)
        {
            return await base.SoraApi.UploadGroupFile(this.Id, localFilePath,
                                                      fileName, floderId);
        }

        #endregion

        #endregion

        #region 群管理方法

        /// <summary>
        /// 设置群组成员禁言
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="duration">
        /// <para>禁言时长(s)</para>
        /// <para>至少60s</para>
        /// </param>
        public async ValueTask<APIStatusType> EnableGroupMemberMute(long userId, long duration)
        {
            return await base.SoraApi.EnableGroupMemberMute(this.Id, userId, duration);
        }

        /// <summary>
        /// 解除群组成员禁言
        /// </summary>
        /// <param name="userId">用户id</param>
        public async ValueTask<APIStatusType> DisableGroupMemberMute(long userId)
        {
            return await base.SoraApi.DisableGroupMemberMute(this.Id, userId);
        }

        /// <summary>
        /// 群组全员禁言
        /// </summary>
        public async ValueTask<APIStatusType> EnableGroupMute()
        {
            return await base.SoraApi.EnableGroupMute(this.Id);
        }

        /// <summary>
        /// 解除群组全员禁言
        /// </summary>s
        public async ValueTask<APIStatusType> DisableGroupMute()
        {
            return await base.SoraApi.DisableGroupMute(this.Id);
        }

        /// <summary>
        /// 设置群成员专属头衔
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="specialTitle">
        /// <para>专属头衔</para>
        /// <para>当值为 <see langword="null"/> 或 <see cref="string"/>.<see langword="Empty"/> 时为清空名片</para>
        /// </param>
        public async ValueTask<APIStatusType> SetGroupMemberSpecialTitle(long userId, string specialTitle)
        {
            return await base.SoraApi.SetGroupMemberSpecialTitle(this.Id, userId, specialTitle);
        }

        /// <summary>
        /// 设置群名片
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="card">
        /// <para>新名片</para>
        /// <para>当值为 <see langword="null"/> 或 <see cref="string"/>.<see langword="Empty"/> 时为清空名片</para>
        /// </param>
        public async ValueTask<APIStatusType> SetGroupCard(long userId, string card)
        {
            return await base.SoraApi.SetGroupCard(this.Id, userId, card);
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="userId">成员id</param>
        public async ValueTask<APIStatusType> EnableGroupAdmin(long userId)
        {
            return await base.SoraApi.EnableGroupAdmin(this.Id, userId);
        }

        /// <summary>
        /// 取消群管理员
        /// </summary>
        /// <param name="userId">成员id</param>
        public async ValueTask<APIStatusType> DisableGroupAdmin(long userId)
        {
            return await base.SoraApi.DisableGroupAdmin(this.Id, userId);
        }

        /// <summary>
        /// 退出群
        /// </summary>
        public async ValueTask<APIStatusType> LeaveGroup()
        {
            return await base.SoraApi.LeaveGroup(this.Id);
        }

        /// <summary>
        /// 解散群
        /// </summary>
        public async ValueTask<APIStatusType> DismissGroup()
        {
            return await base.SoraApi.DismissGroup(this.Id);
        }

        /// <summary>
        /// 群组踢人
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="rejectRequest">拒绝此人的加群请求</param>
        public async ValueTask<APIStatusType> KickGroupMember(long userId, bool rejectRequest = false)
        {
            return await base.SoraApi.KickGroupMember(this.Id, userId, rejectRequest);
        }

        #region Go扩展

        /// <summary>
        /// 设置群名
        /// </summary>
        /// <param name="newName">新群名</param>
        public async ValueTask<APIStatusType> SetGroupName(string newName)
        {
            return await base.SoraApi.SetGroupName(this.Id, newName);
        }

        /// <summary>
        /// 设置群头像
        /// </summary>
        /// <param name="imageFile">图片名/绝对路径/URL/base64</param>
        /// <param name="useCache">是否使用缓存</param>
        public async ValueTask<APIStatusType> SetGroupPortrait(string imageFile, bool useCache = true)
        {
            return await base.SoraApi.SetGroupPortrait(this.Id, imageFile, useCache);
        }

        /// <summary>
        /// 获取群@全体成员剩余次数
        /// </summary>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="canAt"/> 是否可以@全体成员</para>
        /// <para><see langword="groupRemain"/> 群内所有管理当天剩余@全体成员次数</para>
        /// <para><see langword="botRemain"/> BOT当天剩余@全体成员次数</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, bool canAt, short groupRemain, short botRemain)>
            GetGroupAtAllRemain()
        {
            return await base.SoraApi.GetGroupAtAllRemain(this.Id);
        }

        #endregion

        #endregion

        #region 转换方法

        /// <summary>
        /// 定义将 <see cref="Group"/> 对象转换为 <see cref="long"/>
        /// </summary>
        /// <param name="value">转换的 <see cref="Group"/> 对象</param>
        public static implicit operator long(Group value)
        {
            return value.Id;
        }

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(Group groupL, Group groupR)
        {
            if (groupL is null && groupR is null) return true;

            return groupL is not null && groupR is not null && groupL.Id == groupR.Id &&
                   groupL.SoraApi                                        == groupR.SoraApi;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(Group groupL, Group groupR)
        {
            return !(groupL == groupR);
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Group api)
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
            return HashCode.Combine(Id, SoraApi);
        }

        #endregion
    }
}