using System;
using System.Threading.Tasks;

namespace Sora.Util;

/// <summary>
/// 扩展方法
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 运行并捕捉错误
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="this">需要被捕捉错误的方法</param>
    /// <param name="block">错误处理动作</param>
    /// <returns>T</returns>
    public static async ValueTask<T> RunCatch<T>(this ValueTask<T> @this, Func<Exception, T> block)
    {
        try
        {
            return await @this;
        }
        catch (Exception ex)
        {
            block(ex);
            return default;
        }
    }

    /// <summary>
    /// 运行并捕捉错误
    /// </summary>
    /// <param name="this">需要被捕捉错误的方法</param>
    /// <param name="block">错误处理动作</param>
    public static async ValueTask RunCatch(this ValueTask @this, Action<Exception> block)
    {
        try
        {
            await @this;
        }
        catch (Exception ex)
        {
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
    public static async Task<T> RunCatch<T>(this Task<T> @this, Func<Exception, T> block)
    {
        try
        {
            return await @this;
        }
        catch (Exception ex)
        {
            block(ex);
            return default;
        }
    }

    /// <summary>
    /// 运行并捕捉错误
    /// </summary>
    /// <param name="this">需要被捕捉错误的方法</param>
    /// <param name="block">错误处理动作</param>
    public static async Task RunCatch(this Task @this, Action<Exception> block)
    {
        try
        {
            await @this;
        }
        catch (Exception ex)
        {
            block.Invoke(ex);
        }
    }
}