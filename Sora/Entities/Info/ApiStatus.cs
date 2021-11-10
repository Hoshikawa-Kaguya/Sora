using Sora.Enumeration.ApiType;

namespace Sora.Entities.Info;

/// <summary>
/// API执行状态
/// </summary>
public readonly struct ApiStatus
{
    /// <summary>
    /// API返回代码
    /// </summary>
    public ApiStatusType RetCode { get; internal init; }

    /// <summary>
    /// API返回信息
    /// </summary>
    public string ApiMessage { get; internal init; }

    /// <summary>
    /// API返回状态字符串
    /// </summary>
    public string ApiStatusStr { get; internal init; }
}