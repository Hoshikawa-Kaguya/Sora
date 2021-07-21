using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.Info;
using Sora.Entities.MessageElement;
using Sora.Enumeration.ApiType;

namespace Sora.Entities
{
    /// <summary>
    /// 用户类
    /// </summary>
    public sealed class User : BaseModel
    {
        #region 属性

        /// <summary>
        /// 当前实例的用户ID
        /// </summary>
        public long Id { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="connectionId">服务器连接标识</param>
        /// <param name="uid">用户ID</param>
        internal User(Guid serviceId, Guid connectionId, long uid) : base(serviceId, connectionId)
        {
            Id = uid;
        }

        #endregion

        #region 常用操作方法

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="message">
        /// <para>消息</para>
        /// <para><see cref="List{T}"/>(T = <see cref="CQCode"/>)</para>
        /// <para>其他类型的消息会被强制转换为纯文本</para>
        /// </param>
        /// <param name="timeout">覆盖原有超时</param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see langword="messageId"/> 消息ID</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, int messageId)> SendPrivateMessage(
            MessageBody message, TimeSpan? timeout = null)
            => await SoraApi.SendPrivateMessage(Id, message, timeout);

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="useCache"></param>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// <para><see cref="UserInfo"/> 群成员信息</para>
        /// <para><see cref="string"/> qid</para>
        /// </returns>
        public async ValueTask<(ApiStatus apiStatus, UserInfo userInfo, string qid)> GetUserInfo(
            bool useCache = true)
            => await SoraApi.GetUserInfo(Id, useCache);

        /// <summary>
        /// 删除好友
        /// </summary>
        /// <returns>
        /// <para><see cref="ApiStatusType"/> API执行状态</para>
        /// </returns>
        public async ValueTask<ApiStatus> DeleteFriend()
            => await SoraApi.DeleteFriend(Id);

        #endregion

        #region CQ码方法

        /// <summary>
        /// 获取At的CQ码
        /// </summary>
        /// <returns>
        /// <see cref="CQCode"/> AT
        /// </returns>
        public CQCode CQCodeAt()
            => CQCodes.CQAt(Id);

        #endregion

        #region 转换方法

        /// <summary>
        /// 定义将 <see cref="User"/> 对象转换为 <see cref="long"/>
        /// </summary>
        /// <param name="value">转换的 <see cref="User"/> 对象</param>
        public static implicit operator long(User value)
            => value.Id;

        /// <summary>
        /// 定义将 <see cref="User"/> 对象转换为 <see cref="string"/>
        /// </summary>
        /// <param name="value">转换的 <see cref="User"/> 对象</param>
        public static implicit operator string(User value)
            => value.ToString();

        #endregion

        #region 运算符重载

        /// <summary>
        /// 等于重载
        /// </summary>
        public static bool operator ==(User userL, User userR)
        {
            if (userL is null && userR is null) return true;

            return userL is not null && userR is not null && userL.Id == userR.Id && userL.SoraApi == userR.SoraApi;
        }

        /// <summary>
        /// 不等于重载
        /// </summary>
        public static bool operator !=(User userL, User userR)
        {
            return !(userL == userR);
        }

        #endregion

        #region 常用重载

        /// <summary>
        /// 比较重载
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is User user)
            {
                return this == user;
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