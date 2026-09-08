using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sora.Entities.Filters;

namespace Sora.Example.Milky.Filters;

/// <summary>
///     示例：事件后置过滤器 — 仅统计 <see cref="MessageReceivedEvent" /> 的处理耗时。
///     通过设置 <see cref="EventTypes" /> 演示 scope 用法：仅当事件是 MessageReceivedEvent（含子类）才会执行。
/// </summary>
internal sealed class MetricsPostFilter(ILogger logger) : IEventPostFilter
{
    public int Order => 0;

    public Type[]? EventTypes { get; } = [typeof(MessageReceivedEvent)];

    public ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct)
    {
        if (e.PipelineContext is null) return ValueTask.CompletedTask;

        double elapsedMs = Stopwatch.GetElapsedTime(e.PipelineContext.StartTimestamp).TotalMilliseconds;
        logger.LogInformation(
            "[PostFilter] Event {EventType} processed in {Elapsed}ms (chain completed: {ChainCompleted})",
            e.GetType().Name,
            elapsedMs,
            chainCompleted);
        return ValueTask.CompletedTask;
    }
}