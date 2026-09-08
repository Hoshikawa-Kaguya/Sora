using Sora.Example.Milky.Filters;

namespace Sora.Example.Milky.Commands;

/// <summary>
///     最小动态指令示例 — 演示 <see cref="Sora.Command.CommandManager.RegisterDynamicCommand" /> 三种过滤器写法。
///     从 <c>Program.cs</c> 调用 <see cref="Register" /> 完成注册。
/// </summary>
internal class DynamicCommandExamples
{
    /// <summary>注册全部动态指令示例。</summary>
    public void Register(SoraService service)
    {
        // 方式 1：无过滤器
        service.Commands.RegisterDynamicCommand(
            async e => { await Helpers.SendReplyAsync(e, new MessageBody("min dyn cmd")); },
            ["dyn-plain"],
            description: "min impl");

        // 方式 2：lambda + attribute 设置固定值
        service.Commands.RegisterDynamicCommand(
            [Cooldown(5)] async (e) =>
            {
                await Helpers.SendReplyAsync(
                    e,
                    new MessageBody("min dyn cmd with fixed cooldown"));
            },
            ["dyn-cooldown"],
            description: "cmd + [Cooldown(5)]");

        // 方式 3：lambda + 通过 beforeFilters 传入运行时变量
        int runtimeSeconds = Random.Shared.Next(3, 10);
        service.Commands.RegisterDynamicCommand(
            async e =>
            {
                await Helpers.SendReplyAsync(
                    e,
                    new MessageBody($"min dyn cmd with runtime cooldown: ({runtimeSeconds}s)"));
            },
            ["dyn-runtime"],
            description: "cmd + beforeFilters para",
            beforeFilters: [new CooldownAttribute(runtimeSeconds)]);
    }
}