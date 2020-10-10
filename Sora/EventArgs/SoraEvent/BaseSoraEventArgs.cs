using Sora.Model.SoraModel;

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
        /// 接收当前事件的机器人UID
        /// </summary>
        public long SelfId { get; private set; }
        #endregion

        #region 构造函数
        internal BaseSoraEventArgs(SoraApi api, string eventName, long selfId)
        {
            this.SoraApi   = api;
            this.EventName = eventName;
            this.SelfId    = selfId;
        }
        #endregion
    }
}
