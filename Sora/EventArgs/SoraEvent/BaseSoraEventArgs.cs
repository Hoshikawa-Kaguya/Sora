using System;
using Sora.Module.Base;
using Sora.Tool;

namespace Sora.EventArgs.SoraEvent
{
    /// <summary>
    /// 框架事件基类
    /// </summary>
    public abstract class BaseSoraEventArgs : System.EventArgs
    {
        #region 属性
        /// <summary>
        /// 当前事件的API执行实例
        /// </summary>
        public SoraApi SoraApi { get; private set; }

        /// <summary>
        /// 当前事件名
        /// </summary>
        public string EventName { get; private set; }

        /// <summary>
        /// 事件产生时间
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// 接收当前事件的机器人UID
        /// </summary>
        internal long SelfId { get; private set; }

        /// <summary>
        /// 事件产生时间戳
        /// </summary>
        internal long TimeStamp { get; private set; }
        #endregion

        #region 构造函数
        internal BaseSoraEventArgs(Guid connectionGuid, string eventName, long selfId, long time)
        {
            this.SoraApi   = new SoraApi(connectionGuid);
            this.EventName = eventName;
            this.SelfId    = selfId;
            this.TimeStamp = time;
            this.Time      = Utils.TimeStampToDateTime(time);
        }
        #endregion

        #region 基类方法
        /// <summary>
        /// 获取当前登陆账号的ID
        /// </summary>
        public long GetLoginUserId() => this.SelfId;
        #endregion
    }
}
