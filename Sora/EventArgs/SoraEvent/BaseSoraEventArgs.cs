using System;
using System.Threading.Tasks;
using Sora.Command;
using Sora.Entities.Base;
using Sora.Enumeration;
using Sora.OnebotInterface;
using Sora.OnebotModel.OnebotEvent.MessageEvent;
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
        private long TimeStamp { get; set; }

        /// <summary>
        /// <para>是否在处理本次事件后再次触发其他事件，默认为不触发</para>
        /// <para>如:处理Command后可以将此值设置为<see langword="false"/>来阻止后续的事件触发，为<see langword="true"/>时则会触发其他相匹配的指令和事件</para>
        /// <para>如果出现了不同表达式同时被触发且优先级相同的情况，则这几个指令的执行顺序将是不确定的，请避免这种情况的发生</para>
        /// </summary>
        public bool IsContinueEventChain { get; set; }

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
            SoraApi              = new SoraApi(connectionGuid);
            EventName            = eventName;
            LoginUid             = loginUid;
            TimeStamp            = time;
            Time                 = time.ToDateTime();
            IsContinueEventChain = false;
        }

        #endregion

        internal object WaitForUser(long sourceUid, string[] commandExps, MatchType matchType, long sourceGroup = 0)
        {
            IsContinueEventChain = false;

            var waitInfo = CommandManager.GenWaitingCommandInfo(sourceUid, sourceGroup, commandExps, matchType);
            waitInfo.ConnectionId = SoraApi.ConnectionGuid;
            StaticVariable.WaitingDict.Add(waitInfo);
            waitInfo.Semaphore.WaitOne();


            return waitInfo.EventArgs;
        }
    }
}