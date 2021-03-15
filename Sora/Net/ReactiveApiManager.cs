using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.OnebotModel.ApiParams;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;

namespace Sora.Net
{
    /// <summary>
    /// 用于管理和发送API请求
    /// </summary>
    internal static class ReactiveApiManager
    {
        #region 静态属性

        /// <summary>
        /// API超时时间
        /// </summary>
        internal static uint TimeOut { get; set; }

        #endregion

        #region 请求表

        /// <summary>
        /// 暂存数据结构定义
        /// </summary>
        private class ApiData
        {
            internal Guid ConnectionGuid;

            internal Guid Echo;

            internal JObject Response;

            internal DateTime CreateTime;
        }

        /// <summary>
        /// API请求表
        /// </summary>
        private static readonly Dictionary<Guid, ApiData> RequestList = new();

        /// <summary>
        /// API响应被观察者
        /// </summary>
        private static readonly Subject<Guid> ApiSubject = new();

        #endregion

        #region 通信

        /// <summary>
        /// 获取到API响应
        /// </summary>
        /// <param name="echo">标识符</param>
        /// <param name="response">响应json</param>
        internal static void GetResponse(Guid echo, JObject response)
        {
            lock (RequestList)
            {
                if (RequestList.TryGetValue(echo, out var connection))
                {
                    Log.Debug("Sora|ReactiveApiManager", $"Get api response {response.ToString(Formatting.None)}");
                    connection.Response = response;
                    ApiSubject.OnNext(echo);
                }
            }
        }

        /// <summary>
        /// 向API客户端发送请求数据
        /// </summary>
        /// <param name="apiRequest">请求信息</param>
        /// <param name="connectionGuid">服务器连接标识符</param>
        /// <param name="timeout">覆盖原有超时,在不为空时有效</param>
        /// <returns>API返回</returns>
        internal static async ValueTask<JObject> SendApiRequest(ApiRequest apiRequest, Guid connectionGuid,
                                                                int? timeout = null)
        {
            //添加新的请求记录
            lock (RequestList)
            {
                RequestList.Add(apiRequest.Echo, new ApiData
                {
                    ConnectionGuid = connectionGuid,
                    Echo           = apiRequest.Echo,
                    Response       = null,
                    CreateTime     = DateTime.Now
                });
            }

            //向客户端发送请求数据
            if (!ConnectionManager.SendMessage(connectionGuid, JsonConvert.SerializeObject(apiRequest, Formatting.None))
            ) return null;
            //等待客户端返回调用结果
            var responseGuid = await ApiSubject
                                     .Where(guid => guid == apiRequest.Echo)
                                     .Select(guid => guid)
                                     .Take(1)
                                     .Timeout(TimeSpan.FromMilliseconds(timeout ?? (int) TimeOut))
                                     .Catch(Observable.Return(Guid.Empty))
                                     .ToTask()
                                     .RunCatch(e =>
                                               {
                                                   Log.Error("Sora|ReactiveApiManager",
                                                             $"ApiSubject Error {Log.ErrorLogBuilder(e)}");
                                                   return Guid.Empty;
                                               });
            Log.Debug("Sora|ReactiveApiManager", "observer time out");
            lock (RequestList)
            {
                if (RequestList.TryGetValue(apiRequest.Echo, out var connection)) //查找返回值
                {
                    Log.Debug("Sora|ReactiveApiManager", $"Get [{apiRequest.Echo}]");
                    RequestList.Remove(apiRequest.Echo);
                    return connection.Response;
                }
                else
                {
                    Log.Warning("Sora|ReactiveApiManager", "api time out");
                    return null;
                }
            }
        }

        #endregion

        #region 数据处理

        /// <summary>
        /// 清空API请求记录
        /// </summary>
        internal static void ClearApiReqList()
        {
            lock (RequestList)
            {
                Log.Debug("Sora|ReactiveApiManager", $"Force Clean All Requests [{RequestList.Count}]");
                RequestList.Clear();
            }
        }

        /// <summary>
        /// 清理无效的API请求记录
        /// </summary>
        internal static void CleanApiReqList()
        {
            lock (RequestList)
            {
                var oldCount = RequestList.Count;
                var removedKeys = RequestList.Where(p => DateTime.Now - p.Value.CreateTime > TimeSpan.FromMilliseconds(TimeOut)).Select(p => p.Key).ToList();
                foreach (var key in removedKeys)
                {
                    RequestList.Remove(key);
                }
                Log.Debug("Sora|ReactiveApiManager", $"Clean Invalid Requests [{removedKeys.Count}]");
            }
        }

        #endregion
    }
}
