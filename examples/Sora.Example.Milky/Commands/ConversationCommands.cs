using Sora.Entities.MessageWaiting;

namespace Sora.Example.Milky.Commands;

/// <summary>
///     连续对话指令组 — 展示 WaitForNextMessageAsync 的多步骤交互。
///     AI-Generate
/// </summary>
[CommandGroup(Name = "conversation", Prefix = "/")]
public static class ConversationCommands
{
    /// <summary>
    ///     /order — 模拟点单流程（选饮料 → 选规格 → 确认）。
    ///     展示多轮 WaitForNextMessageAsync + 带模式匹配的等待。
    /// </summary>
    [Command(Expressions = ["order"], MatchType = MatchType.Full, Description = "点单对话")]
    public static async ValueTask Order(MessageReceivedEvent e)
    {
        e.IsContinueEventChain = false;
        // 第 1 步：选择饮料
        await Helpers.SendReplyAsync(e, new MessageBody("☕ 欢迎点单！请选择饮料：\n1. 拿铁\n2. 美式\n3. 抹茶"));

        MessageReceivedEvent? step1 = await e.WaitForNextMessageAsync(
                ["1", "2", "3"],
            MatchType.Full,
            TimeSpan.FromSeconds(30));

        if (step1 is null)
        {
            await Helpers.SendReplyAsync(e, new MessageBody("⏰ 点单超时，下次再来吧！"));
            return;
        }

        string drink = step1.Message.Body.GetText().Trim() switch
                           {
                               "1" => "拿铁",
                               "2" => "美式",
                               "3" => "抹茶",
                               _   => "未知"
                           };

        // 第 2 步：选择规格
        await Helpers.SendReplyAsync(step1, new MessageBody($"你选了 {drink}，请选择规格：\n1. 小杯\n2. 中杯\n3. 大杯"));

        MessageReceivedEvent? step2 = await step1.WaitForNextMessageAsync(
                ["1", "2", "3"],
            MatchType.Full,
            TimeSpan.FromSeconds(30));

        if (step2 is null)
        {
            await Helpers.SendReplyAsync(step1, new MessageBody("⏰ 选择超时，订单已取消。"));
            return;
        }

        string size = step2.Message.Body.GetText().Trim() switch
                          {
                              "1" => "小杯",
                              "2" => "中杯",
                              "3" => "大杯",
                              _   => "未知"
                          };

        // 第 3 步：确认
        await Helpers.SendReplyAsync(step2, new MessageBody($"✅ 订单确认：{size}{drink}\n感谢惠顾！☕"));
    }

    /// <summary>
    ///     /quiz — 简单问答游戏，3 题后统计分数。
    ///     展示循环中的 WaitForNextMessageAsync 和自定义 predicate 等待。
    /// </summary>
    [Command(Expressions = ["quiz"], MatchType = MatchType.Full, Description = "问答游戏")]
    public static async ValueTask Quiz(MessageReceivedEvent e)
    {
        e.IsContinueEventChain = false;
        (string Question, string Answer)[] questions =
            [
                ("🧮 1 + 1 = ?", "2"),
                ("🌍 地球是什么形状？（输入：圆/方）", "圆"),
                ("🐱 猫有几条腿？", "4")
            ];

        int score = 0;

        await Helpers.SendReplyAsync(e, new MessageBody("🎮 问答游戏开始！共 3 题，每题 15 秒。\n"));

        MessageReceivedEvent current = e;
        for (int i = 0; i < questions.Length; i++)
        {
            await Helpers.SendReplyAsync(current, new MessageBody($"第 {i + 1} 题：{questions[i].Question}"));

            // 等待任意回复（不限定内容）
            MessageReceivedEvent? answer = await current.WaitForNextMessageAsync(TimeSpan.FromSeconds(15));

            if (answer is null)
            {
                await Helpers.SendReplyAsync(current, new MessageBody($"⏰ 超时！正确答案是：{questions[i].Answer}"));
                continue;
            }

            string userAnswer = answer.Message.Body.GetText().Trim();
            if (string.Equals(userAnswer, questions[i].Answer, StringComparison.OrdinalIgnoreCase))
            {
                score++;
                await Helpers.SendReplyAsync(answer, new MessageBody("✅ 回答正确！"));
            }
            else
            {
                await Helpers.SendReplyAsync(answer, new MessageBody($"❌ 回答错误，正确答案是：{questions[i].Answer}"));
            }

            current = answer;
        }

        // 总结
        string summary = score switch
                             {
                                 3 => $"🏆 满分！你答对了 {score}/3 题，太厉害了！",
                                 2 => $"👍 不错，你答对了 {score}/3 题！",
                                 1 => $"💪 你答对了 {score}/3 题，继续加油！",
                                 _ => $"😅 你答对了 {score}/3 题，下次再接再厉！"
                             };
        await Helpers.SendReplyAsync(current, new MessageBody(summary));
    }
}