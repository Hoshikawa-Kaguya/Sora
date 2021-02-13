using System;
using System.Threading.Tasks;
using YukariToolBox.Console;

namespace Sora.Extensions
{
    /// <summary>
    /// 用于异步执行的简易化错误处理
    /// </summary>
    public static class AsyncExtensions
    {
        /// <summary>
        /// 运行并捕捉错误
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="this">需要被捕捉错误的方法</param>
        /// <param name="block">错误处理动作</param>
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
                    ConsoleLog.Fatal("Sora",ConsoleLog.ErrorLogBuilder(ex));
                return default;
            }
        }

        /// <summary>
        /// 运行并捕捉错误
        /// </summary>
        /// <param name="this">需要被捕捉错误的方法</param>
        /// <param name="block">错误处理动作</param>
        public static async ValueTask RunCatch(this ValueTask @this, Action<Exception> block = null)
        {
            try
            {
                await @this;
            }
            catch (Exception ex)
            {
                if (block == null)
                    ConsoleLog.Fatal("Sora", ConsoleLog.ErrorLogBuilder(ex));
                else
                    block.Invoke(ex);
            }
        }

        /// <summary>
        /// 运行并捕捉错误
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="this">需要被捕捉错误的方法</param>
        /// <param name="block">错误处理动作</param>
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
                    ConsoleLog.Fatal("Sora",ConsoleLog.ErrorLogBuilder(ex));
                return default;
            }
        }

        /// <summary>
        /// 运行并捕捉错误
        /// </summary>
        /// <param name="this">需要被捕捉错误的方法</param>
        /// <param name="block">错误处理动作</param>
        public static async Task RunCatch(this Task @this, Action<Exception> block = null)
        {
            try
            {
                await @this;
            }
            catch (Exception ex)
            {
                if (block == null)
                    ConsoleLog.Fatal("Sora", ConsoleLog.ErrorLogBuilder(ex));
                else
                    block.Invoke(ex);
            }
        }
    }
}
