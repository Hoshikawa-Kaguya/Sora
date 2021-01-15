using System;
using System.Threading.Tasks;
using Sora.Tool;

namespace Sora.Extensions
{
    /// <summary>
    /// 用于异步执行的简易化错误处理
    /// </summary>
    public static class AsyncExtensions
    {
        /// <summary>
        /// 运行并检查错误
        /// </summary>
        /// <typeparam name="T">out type</typeparam>
        /// <param name="this">run method</param>
        /// <param name="block">error info out method</param>
        /// <returns>T</returns>
        public static async ValueTask<T> RunCatch<T>(this ValueTask<T> @this, Func<Exception, T> block = null)
        {
            try
            {
                return await @this;
            }
            catch (Exception ex)
            {
                if (block != null)
                    block(ex);
                else
                    ConsoleLog.Fatal("unknown error",ConsoleLog.ErrorLogBuilder(ex));
                return default;
            }
        }

        /// <summary>
        /// 运行并检查错误
        /// </summary>
        /// <param name="this">run method</param>
        /// <param name="block">error info out method</param>
        public static async ValueTask RunCatch(this ValueTask @this, Action<Exception> block = null)
        {
            try
            {
                await @this;
            }
            catch (Exception ex)
            {
                if (block == null)
                    ConsoleLog.Fatal("unknown error", ConsoleLog.ErrorLogBuilder(ex));
                else
                    block.Invoke(ex);
            }
        }

        /// <summary>
        /// 运行并检查错误
        /// </summary>
        /// <typeparam name="T">out type</typeparam>
        /// <param name="this">run method</param>
        /// <param name="block">error info out method</param>
        /// <returns>T</returns>
        public static async Task<T> RunCatch<T>(this Task<T> @this, Func<Exception, T> block = null)
        {
            try
            {
                return await @this;
            }
            catch (Exception ex)
            {
                if (block != null)
                    block(ex);
                else
                    ConsoleLog.Fatal("unknown error",ConsoleLog.ErrorLogBuilder(ex));
                return default;
            }
        }

        /// <summary>
        /// 运行并检查错误
        /// </summary>
        /// <param name="this">run method</param>
        /// <param name="block">error info out method</param>
        public static async Task RunCatch(this Task @this, Action<Exception> block = null)
        {
            try
            {
                await @this;
            }
            catch (Exception ex)
            {
                if (block == null)
                    ConsoleLog.Fatal("unknown error", ConsoleLog.ErrorLogBuilder(ex));
                else
                    block.Invoke(ex);
            }
        }
    }
}
