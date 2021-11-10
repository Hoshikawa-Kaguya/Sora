using System.ComponentModel;

namespace Sora.Enumeration.ApiType;

/// <summary>
/// API返回值
/// </summary>
[DefaultValue(UnknownStatus)]
public enum ApiStatusType
{
    /// <summary>
    /// API执行成功
    /// </summary>
    OK = 0,

    /// <summary>
    /// API执行失败
    /// </summary>
    Failed = 100,

    /// <summary>
    /// 403
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// 404
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// API执行发生内部错误
    /// </summary>
    Error = 502,

    /// <summary>
    /// API超时
    /// </summary>
    TimeOut = -1,

    /// <summary>
    /// API没有返回任何信息
    /// </summary>
    NullResponse = -2,

    /// <summary>
    /// WS通信失败
    /// </summary>
    SocketSendError = -3,

    /// <summary>
    /// 未知错误
    /// </summary>
    ObservableError = -4,

    /// <summary>
    /// 未知状态
    /// </summary>
    UnknownStatus = -5
}