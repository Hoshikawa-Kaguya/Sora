using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Sora.Attributes;
using Sora.Attributes.Command;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Sora.Command;

internal static class CommandUtils
{
#region 检查方法

    /// <summary>
    /// 检查方法合法性
    /// </summary>
    /// <param name="method">方法信息</param>
    /// <returns>方法是否合法</returns>
    [Reviewed("XiaoHe321", "2021-03-28 20:45")]
    internal static bool CheckCommandMethodLegality(this MethodInfo method)
    {
        //获取指令属性
        SoraCommand commandAttr = method.GetCustomAttribute(typeof(SoraCommand)) as SoraCommand ?? null;

        if (commandAttr is null)
            return false;

        //检查是否设置过表达式
        if (commandAttr.CommandExpressions is null || commandAttr.CommandExpressions.Length == 0)
        {
            Log.Warning("CommandCheck", $"{method.Name}未设置表达式(CommandExpressions)或匹配方法(CommandMatchFunc),已自动忽略");
            return false;
        }

        //源检查
        if (!Enum.IsDefined(commandAttr.SourceType))
        {
            Log.Warning("CommandCheck", $"指令{method.Name}设置了不支持的消息源类型({commandAttr.SourceType}),已自动忽略");
            return false;
        }

        return method.IsDefined(typeof(SoraCommand), false)
               && method.GetParameters().Length == 1
               && method.GetParameters()
                        .Any(para => ParameterCheck<BaseMessageEventArgs>(para, method.Name));
    }

    public static bool IsEmpty(this long[] arr)
    {
        return arr is null || arr.Length == 0;
    }

    private static bool ParameterCheck<TPara>(ParameterInfo parameter, string name)
    {
        bool check = parameter.ParameterType == typeof(TPara) && !parameter.IsOut;
        if (!check)
        {
            StringBuilder log = new();
            log.AppendLine($"指令[{name}]参数类型错误");
            log.AppendLine($"应为{typeof(TPara).Name}实际为{parameter.ParameterType.Name}");
            log.Append("已自动跳过该指令");
            Log.Warning("CommandCheck", log.ToString());
        }

        return check;
    }

#endregion

#region 连续对话指令生成

    /// <summary>
    /// 生成连续对话上下文
    /// </summary>
    /// <param name="sourceUid">消息源UID</param>
    /// <param name="sourceGroup">消息源GID</param>
    /// <param name="matchFunc">自定义匹配方法</param>
    /// <param name="sourceFlag">来源标识</param>
    /// <param name="connectionId">连接标识</param>
    /// <param name="serviceId">服务标识</param>
    /// <exception cref="NullReferenceException">表达式为空时抛出异常</exception>
    [NeedReview("ALL")]
    internal static WaitingInfo GenerateWaitingCommandInfo(long                             sourceUid,
                                                           long                             sourceGroup,
                                                           Func<BaseMessageEventArgs, bool> matchFunc,
                                                           SourceFlag                       sourceFlag,
                                                           Guid                             connectionId,
                                                           Guid                             serviceId)
    {
        if (matchFunc is null)
            throw new ArgumentNullException(nameof(matchFunc));
        return new WaitingInfo(new AutoResetEvent(false),
                               matchFunc,
                               connectionId,
                               serviceId,
                               (sourceUid, sourceGroup),
                               sourceFlag);
    }

    /// <summary>EE
    /// 生成连续对话上下文
    /// </summary>
    /// <param name="sourceUid">消息源UID</param>
    /// <param name="sourceGroup">消息源GID</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="sourceFlag">来源标识</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="connectionId">连接标识</param>
    /// <param name="serviceId">服务标识</param>
    /// <exception cref="NullReferenceException">表达式为空时抛出异常</exception>
    [NeedReview("ALL")]
    internal static WaitingInfo GenerateWaitingCommandInfo(long         sourceUid,
                                                           long         sourceGroup,
                                                           string[]     cmdExps,
                                                           MatchType    matchType,
                                                           SourceFlag   sourceFlag,
                                                           RegexOptions regexOptions,
                                                           Guid         connectionId,
                                                           Guid         serviceId)
    {
        if (cmdExps == null || cmdExps.Length == 0)
            throw new NullReferenceException("cmdExps is empty");
        string[] matchExp = matchType switch
                            {
                                MatchType.Full    => cmdExps.Select(command => $"^{command}$").ToArray(),
                                MatchType.Regex   => cmdExps,
                                MatchType.KeyWord => cmdExps.Select(command => $"({command})+").ToArray(),
                                _                 => throw new NotSupportedException("unknown matchtype")
                            };

        return new WaitingInfo(new AutoResetEvent(false),
                               matchExp,
                               serviceId: serviceId,
                               connectionId: connectionId,
                               source: (sourceUid, sourceGroup),
                               sourceFlag: sourceFlag,
                               regexOptions: regexOptions);
    }

#endregion

    /// <summary>
    /// 处理指令正则表达式
    /// </summary>
    [NeedReview("ALL")]
    internal static string[] ParseCommandExps(string[] cmdExps, string prefix, MatchType matchType)
    {
        switch (matchType)
        {
            case MatchType.Full:
                return cmdExps.Select(command => $"^{prefix}{command}$").ToArray();
            case MatchType.Regex:
                if (!string.IsNullOrEmpty(prefix))
                    Log.Warning("指令初始化", $"当前注册指令类型为正则匹配，自动忽略前缀[{prefix}]");
                return cmdExps;
            case MatchType.KeyWord:
                return cmdExps.Select(command => $"({prefix}{command})+").ToArray();
            default:
                throw new NotSupportedException("unknown matchtype");
        }
    }
}