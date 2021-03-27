using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;
using YukariToolBox.Extensions;

namespace Sora.OnebotInterface
{
    /// <summary>
    /// 静态变量存放区
    /// </summary>
    internal static class StaticVariable
    {
        #region 连续对话上下文

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

            /// <summary>
            /// 比价是否为同一消息来源
            /// </summary>
            internal bool IsSameSource(WaitingInfo info)
            {
                return info.Source       == Source
                    && info.ConnectionId == ConnectionId
                    && info.CommandExpressions.ArrayEquals(CommandExpressions);
            }
        }

        /// <summary>
        /// 连续对话匹配表
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, WaitingInfo> WaitingDict = new();

        #endregion

        #region 正则匹配字段

        internal static readonly List<Regex> FileRegices = new()
        {
            new Regex(@"^(/[^/ ]*)+/?([a-zA-Z0-9]+\.[a-zA-Z0-9]+)$", RegexOptions.Compiled),           //绝对路径-linux/osx
            new Regex(@"^(?:[a-zA-Z]:\/)(?:[^\/|<>?*:""]*\/)*[^\/|<>?*:""]*$", RegexOptions.Compiled), //绝对路径-win
            new Regex(@"^base64:\/\/[\/]?([\da-zA-Z]+[\/+]+)*[\da-zA-Z]+([+=]{1,2}|[\/])?$",
                      RegexOptions.Compiled), //base64
            new Regex(@"^(http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$",
                      RegexOptions.Compiled),                              //网络图片链接
            new Regex(@"^[\w,\s-]+\.[a-zA-Z0-9]+$", RegexOptions.Compiled) //文件名
        };

        #endregion

        #region 响应式API被观察对象

        /// <summary>
        /// API响应被观察对象
        /// </summary>
        internal static readonly Subject<Tuple<Guid, JObject>> ApiSubject = new();

        #endregion

        #region WS静态连接记录表

        /// <summary>
        /// 静态链接表
        /// </summary>
        internal static readonly List<SoraConnectionInfo> ConnectionList = new();

        /// <summary>
        /// 用于存储链接信息和心跳时间的结构体
        /// </summary>
        internal struct SoraConnectionInfo
        {
            internal Guid     ConnectionGuid;
            internal object   Connection;
            internal DateTime LastHeartBeatTime;
            internal long     SelfId;
        }

        #endregion
    }
}