using System;

namespace Sora.Interfaces;

/// <summary>
/// Sora 配置文件
/// </summary>
public interface ISoraConfig
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    string Host { get; init; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    ushort Port { get; init; }

    /// <summary>
    /// 鉴权Token
    /// </summary>
    string AccessToken { get; init; }

    /// <summary>
    /// Universal请求路径
    /// </summary>
    string UniversalPath { get; init; }

    /// <summary>
    /// <para>心跳包超时设置(秒)</para>
    /// <para>此值请不要小于或等于客户端心跳包的发送间隔</para>
    /// </summary>
    TimeSpan HeartBeatTimeOut { get; init; }

    /// <summary>
    /// <para>客户端API调用超时设置(毫秒)</para>
    /// <para>默认为1000无需修改</para>
    /// </summary>
    TimeSpan ApiTimeOut { get; init; }

    /// <summary>
    /// 机器人管理员UID
    /// </summary>
    long[] SuperUsers { get; init; }

    /// <summary>
    /// 不处理来自数组中UID的消息(群聊/私聊)
    /// </summary>
    long[] BlockUsers { get; init; }

    /// <summary>
    /// <para>是否启用Sora自带的指令系统</para>
    /// <para>禁用后EventInterface中的CommandManager将为<see langword="null"/>,同时连续对话的服务也将被禁用</para>
    /// </summary>
    bool EnableSoraCommandManager { get; init; }

    /// <summary>
    /// <para>在触发事件后自动向ob端标记消息已读</para>
    /// <para>仅支持gocq-1.0-beta6以上版本</para>
    /// </summary>
    bool AutoMarkMessageRead { get; init; }
}