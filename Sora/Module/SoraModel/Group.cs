using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Enumeration.ApiEnum;
using Sora.Module.SoraModel.Base;
using Sora.Module.SoraModel.Info;
using Sora.OnebotInterface;

namespace Sora.Module.SoraModel
{
    public sealed class Group : BaseModel
    {
        #region 属性
        /// <summary>
        /// 群号
        /// </summary>
        public long Id { get; private set; }
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
        /// <param name="message">消息内容</param>
        /// <returns>
        /// <para><see cref="APIStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(APIStatusType apiStatus, int messageId)> SendGroupMessage(params object[] message)
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
        /// <para><see cref="GroupInfo"/> 群信息列表</para>
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
        /// <para><see cref="List{T}"/> 群成员列表</para>
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
        public async ValueTask EnableGroupMemberMute(long userId, long duration)
        {
            await base.SoraApi.EnableGroupMemberMute(this.Id, userId, duration);
        }

        /// <summary>
        /// 解除群组成员禁言
        /// </summary>
        /// <param name="userId">用户id</param>
        public async ValueTask DisableGroupMemberMute(long userId)
        {
            await base.SoraApi.DisableGroupMemberMute(this.Id, userId);
        }

        /// <summary>
        /// 群组全员禁言
        /// </summary>
        public async ValueTask EnableGroupMute()
        {
            await base.SoraApi.EnableGroupMute(this.Id);
        }

        /// <summary>
        /// 解除群组全员禁言
        /// </summary>s
        public async ValueTask DisableGroupMute()
        {
            await base.SoraApi.DisableGroupMute(this.Id);
        }

        /// <summary>
        /// 设置群成员专属头衔
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="specialTitle">
        /// <para>专属头衔</para>
        /// <para>当值为 <see langword="null"/> 或 <see cref="string"/>.<see langword="Empty"/> 时为清空名片</para>
        /// </param>
        public async ValueTask SetGroupMemberSpecialTitle(long userId, string specialTitle)
        {
            await base.SoraApi.SetGroupMemberSpecialTitle(this.Id, userId, specialTitle);
        }

        /// <summary>
        /// 设置群名片
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户id</param>
        /// <param name="card">
        /// <para>新名片</para>
        /// <para>当值为 <see langword="null"/> 或 <see cref="string"/>.<see langword="Empty"/> 时为清空名片</para>
        /// </param>
        public async ValueTask SetGroupCard(long userId, string card)
        {
            await base.SoraApi.SetGroupCard(this.Id, userId, card);
        }

        /// <summary>
        /// 设置群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask EnableGroupAdmin(long groupId, long userId)
        {
            await base.SoraApi.EnableGroupAdmin(groupId, userId);
        }

        /// <summary>
        /// 取消群管理员
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="userId">成员id</param>
        public async ValueTask DisableGroupAdmin(long groupId, long userId)
        {
            await base.SoraApi.DisableGroupAdmin(groupId, userId);
        }


        #endregion
    }
}
