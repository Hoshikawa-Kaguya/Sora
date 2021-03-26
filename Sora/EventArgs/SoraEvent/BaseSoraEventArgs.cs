using System;
using Sora.Entities.Base;
using YukariToolBox.Time;

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
        public long LoginUid { get; private set; }

        /// <summary>
        /// 事件产生时间戳
        /// </summary>
        internal long TimeStamp { get; private set; }

        /// <summary>
        /// <para>是否在处理本次事件后再次触发其他事件，默认为不触发</para>
        /// <para>如:处理Command后可以将此值设置为<see langword="false"/>来阻止后续的事件触发</para>
        /// <para>但如有多个Command同时被触发时，只要有一次事件处理后此值为<see langword="true"/>，则无论如何都将触发后续事件</para>
        /// </summary>
        public bool TriggerAfterThis { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="loginUid">当前使用的QQ号</param>
        /// <param name="time">连接时间</param>
        internal BaseSoraEventArgs(Guid connectionGuid, string eventName, long loginUid, long time)
        {
            SoraApi          = new SoraApi(connectionGuid);
            EventName        = eventName;
            LoginUid         = loginUid;
            TimeStamp        = time;
            Time             = time.ToDateTime();
            TriggerAfterThis = false;
        }

        #endregion
    }
}