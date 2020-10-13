using System;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.NoticeEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    public sealed class HonorEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 荣誉获得者
        /// </summary>
        public User TargetUser { get; private set; }

        /// <summary>
        /// 消息源群
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 荣誉类型
        /// </summary>
        public HonorType Honor { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="honorEvent">荣誉类型</param>
        internal HonorEventArgs(Guid connectionGuid, string eventName, ApiHonorEventArgs honorEvent) :
            base(connectionGuid, eventName, honorEvent.SelfID, honorEvent.Time)
        {
            this.TargetUser  = new User(connectionGuid, honorEvent.UserId);
            this.SourceGroup = new Group(connectionGuid, honorEvent.GroupId);
            this.Honor       = honorEvent.HonorType;
        }
        #endregion
    }
}
