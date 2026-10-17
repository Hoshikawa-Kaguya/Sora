using Sora.Example.Milky.Filters;

namespace Sora.Example.Milky.Commands;

/// <summary>
///     最小动态指令示例 — 演示 <see cref="ICommandRegistrar.RegisterDynamicCommand" /> 三种过滤器写法。
///     从 <c>Program.cs</c> 取得程序集注册的 <see cref="AssemblyRegistration.Registrar" /> 后调用
///     <see cref="Register" /> 完成注册；指令归属本类型，可随程序集或本类型启停。
/// </summary>
internal class DynamicCommandExamples
{
    /// <summary>注册全部动态指令示例。</summary>
    /// <param name="registrar">本示例程序集的动态指令注册器。</param>
    public void Register(ICommandRegistrar registrar)
    {
        // 方式 1：无过滤器
        registrar.RegisterDynamicCommand(
            typeof(DynamicCommandExamples),
            async e => { await Helpers.SendReplyAsync(e, new MessageBody("min dyn cmd")); },
            ["dyn-plain"],
            description: "min impl");

        // 方式 2：lambda + attribute 设置固定值
        registrar.RegisterDynamicCommand(
            typeof(DynamicCommandExamples),
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
        registrar.RegisterDynamicCommand(
            typeof(DynamicCommandExamples),
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
