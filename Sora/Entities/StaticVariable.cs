using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using Fleck;
using Newtonsoft.Json.Linq;
using Sora.Enumeration;
using Websocket.Client;
using YukariToolBox.Extensions;

namespace Sora.Entities
{
    /// <summary>
    /// 静态变量存放区
    /// </summary>
    internal static class StaticVariable
    {
        #region 连续对话上下文

        /// <summary>
        /// 连续对话匹配表
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, WaitingInfo> WaitingDict = new();

        #endregion

        #region 正则匹配字段

        internal static readonly Dictionary<CQFileType, Regex> FileRegices = new()
        {
            //绝对路径-linux/osx
            {
                CQFileType.UnixFile, new Regex(@"^(/[^/ ]*)+/?([a-zA-Z0-9]+\.[a-zA-Z0-9]+)$", RegexOptions.Compiled)
            },
            //绝对路径-win
            {
                CQFileType.WinFile,
                new Regex(@"^(?:[a-zA-Z]:\/)(?:[^\/|<>?*:""]*\/)*[^\/|<>?*:""]*$", RegexOptions.Compiled)
            },
            //base64
            {
                CQFileType.Base64, new Regex(@"^base64:\/\/[\/]?([\da-zA-Z]+[\/+]+)*[\da-zA-Z]+([+=]{1,2}|[\/])?$",
                                             RegexOptions.Compiled)
            },
            //网络图片链接
            {
                CQFileType.Url,
                new
                    Regex(@"^(http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?$",
                          RegexOptions.Compiled)
            },
            //文件名
            {CQFileType.FileName, new Regex(@"^[\w,\s-]+\.[a-zA-Z0-9]+$", RegexOptions.Compiled)}
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
        internal static readonly ConcurrentDictionary<Guid, SoraConnectionInfo> ConnectionList = new();

        #endregion

        #region 数据库结构体

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

        /// <summary>
        /// 用于存储链接信息和心跳时间的结构体
        /// </summary>
        internal struct SoraConnectionInfo
        {
            internal readonly object   Connection;
            internal          DateTime LastHeartBeatTime;
            internal          long     SelfId;
            internal readonly TimeSpan ApiTimeout;
            private readonly  int      HashCode;

            internal SoraConnectionInfo(object connection, DateTime lastHeartBeatTime, long selfId, TimeSpan apiTimeout)
            {
                Connection        = connection;
                LastHeartBeatTime = lastHeartBeatTime;
                SelfId            = selfId;
                ApiTimeout        = apiTimeout;
                HashCode = connection switch
                {
                    IWebSocketConnection serverConnection => serverConnection.ConnectionInfo.Id.GetHashCode(),
                    WebsocketClient client => client.GetHashCode(),
                    _ => throw new NotSupportedException("unknown connection type")
                };
            }

            public override int GetHashCode()
            {
                return HashCode;
            }
        }

        #endregion
    }
}