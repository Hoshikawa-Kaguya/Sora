using Microsoft.Extensions.Logging;
using Sora.Entities.Filters;

namespace Sora.Example.Milky.Filters;

/// <summary>
///     示例：事件前置过滤器 — 记录所有到达事件的日志。
///     未设置任何 scope（<c>EventTypes</c> / <c>SourceTypes</c> / <c>Predicate</c>）= 注册即全局生效，
///     等价于"无作用域 = 跑所有事件"。
///     始终返回 true（不拦截），仅做观测用途。
/// </summary>
internal sealed class LoggingPreFilter(ILogger logger) : IEventPreFilter
{
    public int Order => -100;

    public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
    {
        logger.LogDebug(
            "[PreFilter] Event {EventType} from connection {ConnectionId}",
            e.GetType().Name,
            e.ConnectionId);
        return new ValueTask<bool>(true);
    }
}