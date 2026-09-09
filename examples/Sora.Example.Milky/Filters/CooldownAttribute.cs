using Microsoft.Extensions.Logging;
using Sora.Command.Filters;
using Sora.Command.InternalEntities;

namespace Sora.Example.Milky.Filters;

/// <summary>
///     示例：命令前置过滤器（attribute）— 简单的命令冷却（同一用户不能在指定秒数内重复触发同一命令）。
///     用法：在 <c>[Command]</c> 方法上贴 <c>[Cooldown(5)]</c>，或贴在 <c>[CommandGroup]</c> 类上让该组所有命令共享冷却。
/// </summary>
public sealed class CooldownAttribute : CommandBeforeFilterAttribute
{
    private static ILogger Logger => SoraLogger.CreateLogger<CooldownAttribute>();

    private readonly Lock                         _cooldownLock = new();
    private readonly Dictionary<string, DateTime> _cooldowns    = [];

    /// <summary>构造函数：attribute 不能注入 ILogger 等运行时依赖，使用静态 SoraLogger 访问器。</summary>
    /// <param name="seconds">冷却秒数。</param>
    public CooldownAttribute(int seconds)
    {
        Cooldown = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>冷却时长。</summary>
    public TimeSpan Cooldown { get; }

    /// <inheritdoc />
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct)
    {
        string   key = $"{e.Message.SenderId}:{cmd.Method.Name}";
        DateTime now = DateTime.UtcNow;
        lock (_cooldownLock)
        {
            TimeSpan elapsed;
            if (_cooldowns.TryGetValue(key, out DateTime lastExec)
                && (elapsed = now - lastExec) < Cooldown)
            {
                Logger.LogWarning(
                    "[Cooldown] Command {CommandName} blocked for user {UserId} (cooldown active), last exec command duration {Duration} s.",
                    cmd.Method.Name,
                    e.Message.SenderId,
                    Math.Round(elapsed.TotalSeconds, 2));
                return new ValueTask<bool>(false);
            }

            _cooldowns[key] = now;
        }

        return new ValueTask<bool>(true);
    }
}