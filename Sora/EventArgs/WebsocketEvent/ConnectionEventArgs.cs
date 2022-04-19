using System;

namespace Sora.EventArgs.WebsocketEvent;

/// <summary>
/// 服务器连接事件
/// </summary>
public sealed class ConnectionEventArgs : System.EventArgs
{
    #region 属性

    /// <summary>
    /// 客户端类型
    /// </summary>
    public string Role { get; private set; }

    /// <summary>
    /// 机器人登录账号UID
    /// </summary>
    public long SelfId { get; private set; }

    /// <summary>
    /// 链接ID
    /// </summary>
    public Guid ConnectionId { get; private set; }

    #endregion

    #region 构造函数

    internal ConnectionEventArgs(string role, long selfId, Guid id)
    {
        SelfId       = selfId;
        Role         = role;
        ConnectionId = id;
    }

    #endregion
}