using System;
using System.Text.RegularExpressions;
using System.Threading;
using YukariToolBox.Extensions;

namespace Sora.Entities.Info.InternalDataInfo
{
    /// <summary>
    /// 连续对话上下文
    /// </summary>
    internal struct WaitingInfo
    {
        internal readonly AutoResetEvent   Semaphore;
        internal readonly string[]         CommandExpressions;
        internal          object           EventArgs;
        internal readonly Guid             ConnectionId;
        internal readonly RegexOptions     RegexOptions;
        internal readonly (long u, long g) Source;

        /// <summary>
        /// 构造方法
        /// </summary>
        internal WaitingInfo(AutoResetEvent semaphore, string[] commandExpressions, Guid connectionId,
                             (long u, long g) source, RegexOptions regexOptions)
        {
            Semaphore          = semaphore;
            CommandExpressions = commandExpressions;
            ConnectionId       = connectionId;
            Source             = source;
            EventArgs          = null;
            RegexOptions       = regexOptions;
        }

        /// <summary>
        /// 比价是否为同一消息来源
        /// </summary>
        internal bool IsSameSource(WaitingInfo info)
        {
            return info.Source       == Source
                && info.ConnectionId == ConnectionId
                && info.CommandExpressions.ArrayEquals(CommandExpressions);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Semaphore, CommandExpressions, ConnectionId, Source.u, Source.g);
        }
    }
}