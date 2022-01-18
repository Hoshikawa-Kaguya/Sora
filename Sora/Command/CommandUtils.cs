using System.Linq;
using System.Reflection;
using Sora.Attributes;
using Sora.Attributes.Command;
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
    internal static bool CheckMethodLegality(this MethodInfo method)
    {
        //获取指令属性
        RegexCommand commandAttr =
            method.GetCustomAttribute(typeof(RegexCommand)) as RegexCommand ??
            null;

        if (commandAttr is null) return false;

        //检查是否设置过消息源
        if (commandAttr.SourceType == SourceFlag.None)
        {
            Log.Warning("CommandCheck", $"{method.Name}未设置消息源类型(SourceType),已自动忽略");
            return false;
        }

        //检查是否设置过表达式
        if (commandAttr.CommandExpressions is null || commandAttr.CommandExpressions.Length == 0)
        {
            Log.Warning("CommandCheck", $"{method.Name}未设置表达式(CommandExpressions),已自动忽略");
            return false;
        }

        bool isGroupCommandLegality =
            method.IsDefined(typeof(RegexCommand), false)     &&
            method.GetParameters().Length == 1                &&
            commandAttr.SourceType        == SourceFlag.Group &&
            method.GetParameters()
                  .Any(para =>
                       para.ParameterType == typeof(GroupMessageEventArgs) &&
                       !para.IsOut);

        bool isPrivateCommandLegality =
            method.IsDefined(typeof(RegexCommand), false)       &&
            method.GetParameters().Length == 1                  &&
            commandAttr.SourceType        == SourceFlag.Private &&
            method.GetParameters()
                  .Any(para =>
                       para.ParameterType == typeof(PrivateMessageEventArgs) &&
                       !para.IsOut);

        return isGroupCommandLegality || isPrivateCommandLegality;
    }

    #endregion
}