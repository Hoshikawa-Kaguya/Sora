using System;
using System.Linq;
using Sora.Interfaces;

namespace Sora.Net.Config;

/// <summary>
/// <para>服务器配置类</para>
/// <para>所有设置项都有默认值一般不需要配置</para>
/// </summary>
public sealed class ServerConfig : ISoraConfig
{
    #region 私有字段

    /// <summary>
    /// 机器人管理员UID
    /// </summary>
    private readonly long[] _superUsers;

    /// <summary>
    /// 屏蔽用户
    /// </summary>
    private readonly long[] _blockUsers;

    #endregion

    /// <summary>
    /// 反向服务器监听地址
    /// </summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <summary>
    /// 反向服务器端口
    /// </summary>
    public ushort Port { get; init; } = 8080;

    /// <summary>
    /// 鉴权Token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Universal请求路径
    /// </summary>
    public string UniversalPath { get; init; } = string.Empty;

    /// <summary>
    /// 机器人管理员UID
    /// </summary>
    public long[] SuperUsers
    {
        get => _superUsers ?? Array.Empty<long>();
        init
        {
            if (value.Any(uid => uid < 10000)) throw new ArgumentException("uid cannot less than 10000");
            _superUsers = value;
        }
    }

    /// <summary>
    /// 不处理来自数组中UID的消息(群聊/私聊)
    /// </summary>
    public long[] BlockUsers
    {
        get => _blockUsers ?? Array.Empty<long>();
        init
        {
            if (value.Any(uid => uid < 10000)) throw new ArgumentException("uid cannot less than 10000");
            _blockUsers = value;
        }
    }

    /// <summary>
    /// <para>心跳包超时设置(秒)</para>
    /// <para>此值请不要小于或等于客户端心跳包的发送间隔</para>
    /// </summary>
    public TimeSpan HeartBeatTimeOut { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// <para>客户端API调用超时设置(毫秒)</para>
    /// <para>默认为1000无需修改</para>
    /// </summary>
    public TimeSpan ApiTimeOut { get; init; } = TimeSpan.FromMilliseconds(5000);

    /// <summary>
    /// <para>是否启用Sora自带的指令系统</para>
    /// <para>禁用后EventInterface中的CommandManager将为<see langword="null"/>,同时连续对话的服务也将被禁用</para>
    /// </summary>
    public bool EnableSoraCommandManager { get; init; } = true;

    /// <summary>
    /// <para>在触发事件后自动向ob端标记消息已读</para>
    /// <para>仅支持gocq-1.0-beta6以上版本</para>
    /// </summary>
    public bool AutoMarkMessageRead { get; init; } = true;
}