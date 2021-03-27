using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Sora.Attributes.Command;

namespace Sora.OnebotInterface
{
    internal static class StaticVariable
    {
        /// <summary>
        /// 连续对话上下文
        /// </summary>
        internal struct WaitingInfo
        {
            internal AutoResetEvent   Semaphore;
            internal string[]         CommandExpressions;
            internal object           EventArgs;
            internal Guid             ConnectionId;
            internal (long u, long g) Source;

            internal void SetEventArgs(object eventArgs) => EventArgs = eventArgs;
        }
        
        public static readonly ConcurrentQueue<(WaitiableCommand Command, AutoResetEvent ResetEvent)> CommandWaitList
            = new();

        public static readonly ConcurrentDictionary<Guid, WaitingInfo> WaitingDict = new();
    }
}