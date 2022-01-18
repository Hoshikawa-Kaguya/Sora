using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Attributes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.OnebotModel.ApiParams;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Sora.Net;

/// <summary>
/// 用于管理和发送API请求
/// </summary>
internal static class ReactiveApiManager
{
    #region 通信

    /// <summary>
    /// 获取到API响应
    /// </summary>
    /// <param name="echo">标识符</param>
    /// <param name="response">响应json</param>
    internal static void GetResponse(Guid echo, JObject response)
    {
        Log.Debug("Sora", $"Get api response {response.ToString(Formatting.None)}");
        StaticVariable.ApiSubject.OnNext(new Tuple<Guid, JObject>(echo, response));
    }

    /// <summary>
    /// 向API客户端发送请求数据
    /// </summary>
    /// <param name="apiRequest">请求信息</param>
    /// <param name="connectionId">服务器连接标识符</param>
    /// <param name="timeout">覆盖原有超时,在不为空时有效</param>
    /// <returns>API返回</returns>
    [NeedReview("ALL")]
    internal static async ValueTask<(ApiStatus, JObject)> SendApiRequest(
        ApiRequest apiRequest, Guid connectionId, TimeSpan? timeout = null)
    {
        TimeSpan currentTimeout;
        if (timeout is null)
        {
            if (!ConnectionManager.GetApiTimeout(connectionId, out currentTimeout))
            {
                Log.Error("Sora", "Cannot get api timeout");
                currentTimeout = TimeSpan.FromSeconds(5);
            }
        }
        else
        {
            Log.Debug("Sora", $"timeout covered to {timeout.Value.TotalMilliseconds} ms");
            currentTimeout = (TimeSpan) timeout;
        }

        //错误数据
        (bool isTimeout, Exception exception) = (false, null);
        //序列化请求
        string msg = JsonConvert.SerializeObject(apiRequest, Formatting.None);
        //向客户端发送请求数据
        Task<JObject> apiTask = StaticVariable.ApiSubject
                                              .Where(request => request.Item1 == apiRequest.Echo)
                                              .Select(request => request.Item2)
                                              .Take(1)
                                              .Timeout(currentTimeout)
                                              .ToTask()
                                              .RunCatch(e =>
                                               {
                                                   isTimeout = e is TimeoutException;
                                                   exception = e;
                                                   //在错误为超时时不打印log
                                                   if (!isTimeout)
                                                       Log.Error("Sora",
                                                           $"ApiSubject Error {Log.ErrorLogBuilder(e)}");
                                                   return new JObject();
                                               });

        //这里的错误最终将抛给开发者
        //发送消息
        if (!ConnectionManager.SendMessage(connectionId, msg))
            //API消息发送失败
            return (SocketSendError(), null);

        //等待客户端返回调用结果
        JObject response = await apiTask;
        //检查API返回
        if (response != null && response.Count != 0) return (GetApiStatus(response), response);

        //空响应
        if (exception == null) return (NullResponse(), null);
        //观察者抛出异常
        if (isTimeout)
            Log.Error("Sora",
                $"api time out[msg echo:{apiRequest.Echo}]");
        return isTimeout
            ? (TimeOut(), null)
            : (ObservableError(Log.ErrorLogBuilder(exception)), null);
    }

    #endregion

    #region API状态处理

    /// <summary>
    /// 获取API状态返回值
    /// 所有API回调请求都会返回状态值
    /// </summary>
    /// <param name="msg">消息JSON</param>
    [Reviewed("XiaoHe321", "2021-04-13 22:54")]
    private static ApiStatus GetApiStatus(JObject msg)
    {
        return new ApiStatus
        {
            RetCode = Enum.TryParse(msg["retcode"]?.ToString() ?? string.Empty,
                out ApiStatusType messageCode)
                ? messageCode
                : ApiStatusType.UnknownStatus,
            ApiMessage = msg["msg"] == null && msg["wording"] == null
                ? string.Empty
                : $"{msg["msg"] ?? string.Empty}({msg["wording"] ?? string.Empty})",
            ApiStatusStr = msg["status"]?.ToString() ?? "failed"
        };
    }

    private static ApiStatus TimeOut()
    {
        return new ApiStatus
        {
            RetCode      = ApiStatusType.TimeOut,
            ApiMessage   = "api timeout",
            ApiStatusStr = "timeout"
        };
    }

    private static ApiStatus NullResponse()
    {
        return new ApiStatus
        {
            RetCode      = ApiStatusType.NullResponse,
            ApiMessage   = "get null response from api",
            ApiStatusStr = "failed"
        };
    }

    private static ApiStatus SocketSendError()
    {
        return new ApiStatus
        {
            RetCode      = ApiStatusType.SocketSendError,
            ApiMessage   = "cannot send message to api",
            ApiStatusStr = "failed"
        };
    }

    private static ApiStatus ObservableError(string errLog)
    {
        return new ApiStatus
        {
            RetCode      = ApiStatusType.ObservableError,
            ApiMessage   = errLog,
            ApiStatusStr = "observable error"
        };
    }

    #endregion
}