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
        private struct ApiData
        {
            internal Guid ConnectionGuid;

            internal Guid Echo;

            internal JObject Response;

            internal DateTime CreateTime;
        }

        /// <summary>
        /// API请求表
        /// </summary>
        private static readonly List<ApiData> RequestList = new();

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
                if (RequestList.All(guid => guid.Echo != echo)) return;
                Log.Debug("Sora|ReactiveApiManager", $"Get api response {response.ToString(Formatting.None)}");
                var connectionIndex = RequestList.FindIndex(conn => conn.Echo == echo);
                var connection      = RequestList[connectionIndex];
                connection.Response          = response;
                RequestList[connectionIndex] = connection;
                ApiSubject.OnNext(echo);
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
                RequestList.Add(new ApiData
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
            if (responseGuid.Equals(Guid.Empty)) Log.Debug("Sora|ReactiveApiManager", "observer time out");
            lock (RequestList)
            {
                //查找返回值
                var reqIndex = RequestList.FindIndex(apiResponse =>
                                                         apiResponse.Echo           == apiRequest.Echo &&
                                                         apiResponse.ConnectionGuid == connectionGuid);
                Log.Debug("Sora|ReactiveApiManager", $"Get [{apiRequest.Echo}] index [{reqIndex}]");
                if (reqIndex == -1)
                {
                    Log.Warning("Sora|ReactiveApiManager", "api time out");
                    return null;
                }

                var ret = RequestList[reqIndex].Response;
                RequestList.RemoveAt(reqIndex);
                return ret;
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
                RequestList.RemoveAll(req => DateTime.Now - req.CreateTime > TimeSpan.FromMilliseconds(TimeOut));
                Log.Debug("Sora|ReactiveApiManager", $"Clean Invalid Requests [{oldCount - RequestList.Count}]");
            }
        }

        #endregion
    }
}