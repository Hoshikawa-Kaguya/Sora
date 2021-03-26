using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Sora.Attributes.Command;
using Sora.Entities.Info;

namespace Sora.OnebotInterface
{
    internal static class StaticVariable
    {
        public static ConcurrentQueue<(WaitiableCommand Command, AutoResetEvent ResetEvent)> CommandWaitList
            = new ConcurrentQueue<(WaitiableCommand Command, AutoResetEvent ResetEvent)>();
    }
}