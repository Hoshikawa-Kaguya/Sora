using Sora.Enumeration;

namespace Sora.Interfaces;

/// <summary>
/// socket实例的上层发送和部分控制方法映射
/// </summary>
internal interface ISoraSocket
{
    /// <summary>
    /// socket实例
    /// </summary>
    object SocketInstance { get; set; }

    /// <summary>
    /// 连接类型
    /// </summary>
    SoraSocketType SocketType { get; }

    /// <summary>
    /// 向连接发送信息
    /// </summary>
    /// <param name="message">信息</param>
    void Send(string message);

    /// <summary>
    /// 关闭本次连接
    /// </summary>
    void Close();
}