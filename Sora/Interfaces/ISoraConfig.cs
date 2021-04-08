using System;

namespace Sora.Interfaces
{
    /// <summary>
    /// Sora 配置文件
    /// </summary>
    public interface ISoraConfig
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string Host { get; init; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public uint Port { get; init; }

        /// <summary>
        /// 鉴权Token
        /// </summary>
        public string AccessToken { get; init; }

        /// <summary>
        /// Universal请求路径
        /// </summary>
        public string UniversalPath { get; init; }

        /// <summary>
        /// <para>心跳包超时设置(秒)</para>
        /// <para>此值请不要小于或等于客户端心跳包的发送间隔</para>
        /// </summary>
        public TimeSpan HeartBeatTimeOut { get; init; }

        /// <summary>
        /// <para>客户端API调用超时设置(毫秒)</para>
        /// <para>默认为1000无需修改</para>
        /// </summary>
        public TimeSpan ApiTimeOut { get; init; }
        
        /// <summary>
        /// 机器人管理员UID
        /// </summary>
        public long[] SuperUsers { get; init; }

        /// <summary>
        /// 是否启用Sora自带的指令系统
        /// </summary>
        public bool EnableSoraCommandManager { get; init; }

    }
}