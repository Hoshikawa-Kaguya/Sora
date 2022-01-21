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
        SoraCommand commandAttr =
            method.GetCustomAttribute(typeof(SoraCommand)) as SoraCommand ??
            null;

        if (commandAttr is null) return false;

        //检查是否设置过表达式
        if (commandAttr.CommandExpressions is null || commandAttr.CommandExpressions.Length == 0)
        {
            Log.Warning("CommandCheck", $"{method.Name}未设置表达式(CommandExpressions),已自动忽略");
            return false;
        }

        //源检查
        if (commandAttr.SourceType is not SourceFlag.Group or SourceFlag.Private)
        {
            Log.Warning("CommandCheck", $"指令{method.Name}设置了不支持的消息源类型({commandAttr.SourceType}),已自动忽略");
            return false;
        }

        bool preCheck = 
            method.IsDefined(typeof(SoraCommand), false) &&
            method.GetParameters().Length == 1;

        return commandAttr.SourceType switch
        {
            SourceFlag.Group => preCheck &&
                method.GetParameters()
                      .Any(para =>
                           para.ParameterType == typeof(GroupMessageEventArgs) &&
                           !para.IsOut),
            SourceFlag.Private => preCheck &&
                method.GetParameters()
                      .Any(para =>
                           para.ParameterType == typeof(GroupMessageEventArgs) &&
                           !para.IsOut),
            _ => false
        };
    }

    public static bool IsEmpty(this long[] arr)
    {
        return arr is null || arr.Length == 0;
    }

    #endregion
}