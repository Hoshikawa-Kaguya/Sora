using System;
using Sora.Interfaces;

namespace Sora.OnebotModel
{
    /// <summary>
    /// <para>客户端配置类</para>
    /// <para>所有设置项都有默认值一般不需要配置</para>
    /// </summary>
    public class ClientConfig : ISoraConfig
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; init; } = "127.0.0.1";

        /// <summary>
        /// 服务器端口
        /// </summary>
        public uint Port { get; init; } = 6700;

        /// <summary>
        /// 鉴权Token
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;

        /// <summary>
        /// Universal请求路径
        /// </summary>
        public string UniversalPath { get; init; } = string.Empty;

        /// <summary>
        /// <para>心跳包超时设置(秒)</para>
        /// <para>此值请不要小于或等于客户端心跳包的发送间隔</para>
        /// </summary>
        public TimeSpan HeartBeatTimeOut { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// <para>客户端API调用超时设置(毫秒)</para>
        /// <para>默认为1000ms无需修改</para>
        /// </summary>
        public TimeSpan ApiTimeOut { get; init; } = TimeSpan.FromMilliseconds(1000);

        /// <summary>
        /// 是否启用Sora自带的指令系统
        /// </summary>
        public bool EnableSoraCommandManager { get; init; } = true;

        /// <summary>
        /// <para>丢失连接时的重连超时</para>
        /// <para>默认5秒无需修改</para>
        /// </summary>
        public TimeSpan ReconnectTimeOut { get; init; } = TimeSpan.FromSeconds(5);
    }
}