using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;

namespace Sora.Entities
{
    /// <summary>
    /// 静态变量存放区
    /// </summary>
    internal static class StaticVariable
    {
        /// <summary>
        /// 连续对话匹配上下文
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, WaitingInfo> WaitingDict = new();

        /// <summary>
        /// 数据文本匹配正则
        /// </summary>
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

        /// <summary>
        /// API响应被观察对象
        /// </summary>
        internal static readonly Subject<Tuple<Guid, JObject>> ApiSubject = new();

        /// <summary>
        /// WS静态连接记录表
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, SoraConnectionInfo> ConnectionInfos = new();

        /// <summary>
        /// 服务信息
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, ServiceInfo> ServiceInfos = new();
    }
}