using Microsoft.Extensions.Logging;
using Sora.Command.Filters;
using Sora.Command.InternalEntities;

namespace Sora.Example.Milky.Filters;

/// <summary>
///     示例：命令前置过滤器（attribute）— 类级日志注解。
///     贴在 <c>[CommandGroup]</c> 类上时，组内所有命令在执行前都会跑这个 filter；
///     由于 attribute 实例由框架在扫描时缓存（同 group 共享同一实例），<see cref="_invocationCount" /> 会
///     跨组内所有命令累计。
/// </summary>
public sealed class LoggingAttribute : CommandBeforeFilterAttribute
{
    private static ILogger _logger => SoraLogger.CreateLogger<LoggingAttribute>();

    private int _invocationCount;

    /// <inheritdoc />
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        int n = Interlocked.Increment(ref _invocationCount);
        _logger.LogInformation(
            "[Logging] #{N} command [{CommandName}] from {SenderId} in [{SourceType}]",
            n,
            cmd.Method.Name,
            e.Message.SenderId,
            e.Message.SourceType);
        return new ValueTask<bool>(true);
    }
}