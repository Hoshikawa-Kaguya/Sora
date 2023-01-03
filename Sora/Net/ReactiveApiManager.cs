using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Attributes;
using Sora.Entities;
using Sora.Enumeration.ApiType;
using Sora.Net.Records;
using Sora.OnebotModel.ApiParams;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Sora.Net;

/// <summary>
/// 用于管理和发送API请求
/// </summary>
internal static class ReactiveApiManager
{
#region Buffer

    /// <summary>
    /// API响应被观察对象
    /// 结构:Tuple[echo id,响应json]
    /// </summary>
    private static readonly Subject<(Guid id, JObject data)> ApiSubject = new();

#endregion

#region 通信

    /// <summary>
    /// 获取到API响应
    /// </summary>
    /// <param name="echo">标识符</param>
    /// <param name="response">响应json</param>
    internal static void GetResponse(Guid echo, JObject response)
    {
        Log.Debug("Sora", $"Get api response {response.ToString(Formatting.None)}");
        ApiSubject.OnNext((echo, response));
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
        ApiRequest apiRequest,
        Guid       connectionId,
        TimeSpan?  timeout = null)
    {
        TimeSpan currentTimeout;
        if (timeout is null)
        {
            if (!ConnectionRecord.GetApiTimeout(connectionId, out currentTimeout))
            {
                Log.Error("Sora", "无法获取当前api的超时设置");
                currentTimeout = TimeSpan.FromSeconds(5);
            }
        }
        else
        {
            Log.Debug("Sora", $"timeout covered to {timeout.Value.TotalMilliseconds} ms");
            currentTimeout = (TimeSpan)timeout;
        }

        //错误数据
        (bool isTimeout, Exception exception) = (false, null);
        //序列化请求
        string msg     = JsonConvert.SerializeObject(apiRequest, Formatting.None);
        string apiName = Helper.GetFieldDesc(apiRequest.ApiRequestType);
        //向客户端发送请求数据
        Log.Debug("Sora", $"Sending {apiName} request");
        Task<JObject> apiTask = ApiSubject.Where(request => request.id == apiRequest.Echo)
                                          .Select(request => request.data)
                                          .Take(1)
                                          .Timeout(currentTimeout)
                                          .ToTask()
                                          .RunCatch(e =>
                                          {
                                              isTimeout = e is TimeoutException;
                                              exception = e;
                                              //在错误为超时时不打印log
                                              if (!isTimeout)
                                                  Log.Error("Sora", $"ApiSubject 发生错误: {Log.ErrorLogBuilder(e)}");
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
        if (response != null && response.Count != 0)
            return (GetApiStatus(apiName, response), response);

        //空响应
        if (exception == null)
            return (NullResponse(), null);
        //观察者抛出异常
        if (isTimeout)
            Log.Error("Sora", $"API超时[msg echo:{apiRequest.Echo}]");
        return isTimeout ? (TimeOut(), null) : (ObservableError(Log.ErrorLogBuilder(exception)), null);
    }

#endregion

#region API状态处理

    /// <summary>
    /// 获取API状态返回值
    /// 所有API回调请求都会返回状态值
    /// </summary>
    [Reviewed("XiaoHe321", "2021-04-13 22:54")]
    private static ApiStatus GetApiStatus(string apiName, JObject msg)
    {
        string retCode = int.TryParse(msg["retcode"]?.ToString(), out int ret) switch
                         {
                             true when ret < 0 => "100",
                             false             => "-5",
                             _                 => ret.ToString()
                         };

        ApiStatusType apiStatus = Enum.TryParse(retCode, out ApiStatusType messageCode)
            ? messageCode
            : ApiStatusType.UnknownStatus;
        string message = msg["msg"] == null && msg["wording"] == null
            ? string.Empty
            : $"{msg["msg"] ?? string.Empty}({msg["wording"] ?? string.Empty})";
        string statusStr = msg["status"]?.ToString() ?? "failed";

        Log.Debug("Sora", $"Get {apiName} response [{apiStatus}]");

        if (retCode != "0")
        {
            StringBuilder errMsg = new();
            errMsg.AppendLine("gocq api error");
            errMsg.AppendLine($"api: {apiName}");
            errMsg.AppendLine($"retcode: {retCode}");
            errMsg.Append($"message: {message}[{statusStr}]");
            Log.Error("api", errMsg.ToString());
        }

        return new ApiStatus
        {
            RetCode      = apiStatus,
            ApiMessage   = message,
            ApiStatusStr = statusStr
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