using Microsoft.Extensions.Logging;
using Sora.Entities.Filters;

namespace Sora.Example.OneBot11.Filters;

/// <summary>
///     示例：简单黑名单前置过滤器 — 阻止特定用户的消息事件。
///     通过 <see cref="EventTypes" /> 限定为 <see cref="MessageReceivedEvent" />，避免在非消息事件上做无意义的检查。
/// </summary>
internal sealed class SimpleBlacklistFilter(ILogger logger) : IEventPreFilter
{
    // 示例黑名单（实际使用时可从数据库/配置加载）
    private readonly HashSet<long> _blockedUsers = [123456789L];

    public int Order => -200;

    public Type[]? EventTypes { get; } = [typeof(MessageReceivedEvent)];

    public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
    {
        // EventTypes scope 已经保证只对 MessageReceivedEvent 触发
        MessageReceivedEvent msg = (MessageReceivedEvent)e;

        if (_blockedUsers.Contains(msg.Message.SenderId))
        {
            logger.LogDebug("[Blacklist] Blocked event from user {UserId}", msg.Message.SenderId);
            return new ValueTask<bool>(false);
        }

        return new ValueTask<bool>(true);
    }
}