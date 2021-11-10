using System;

namespace Sora.Exceptions;

/// <summary>
/// WebSocket客户端错误
/// </summary>
public class WebSocketClientException : Exception
{
    /// <summary>
    /// 初始化
    /// </summary>
    public WebSocketClientException() : base("Server is running")
    {
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public WebSocketClientException(string message) : base(message)
    {
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public WebSocketClientException(string message, Exception innerException) : base(message, innerException)
    {
    }
}