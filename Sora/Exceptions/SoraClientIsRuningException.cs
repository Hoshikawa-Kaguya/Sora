using System;

namespace Sora.Exceptions
{
    /// <summary>
    /// 客户端已经在运行错误
    /// </summary>
    public class SoraClientIsRuningException : Exception
    {
        /// <summary>
        /// 当前连接的账号id
        /// </summary>
        public long SelfId { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        public SoraClientIsRuningException() : base("Server is running")
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public SoraClientIsRuningException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public SoraClientIsRuningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public SoraClientIsRuningException(string message, long selfId, Exception innerException) :
            base(message, innerException)
        {
            SelfId = selfId;
        }

        internal SoraClientIsRuningException(string message, long selfId) :
            base(message)
        {
            SelfId = selfId;
        }
    }
}