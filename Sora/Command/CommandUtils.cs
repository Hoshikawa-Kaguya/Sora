using System.Linq;
using System.Reflection;
using Sora.Attributes;
using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;

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
        var isGroupCommandLegality = method.IsDefined(typeof(GroupCommand), false) &&
                                     method.GetParameters().Length == 1            &&
                                     method.GetParameters()
                                           .Any(para =>
                                                    para.ParameterType == typeof(GroupMessageEventArgs) &&
                                                    !para.IsOut);

        var isPrivateCommandLegality = method.IsDefined(typeof(PrivateCommand), false) &&
                                       method.GetParameters().Length == 1              &&
                                       method.GetParameters()
                                             .Any(para =>
                                                      para.ParameterType == typeof(PrivateMessageEventArgs) &&
                                                      !para.IsOut);

        return isGroupCommandLegality || isPrivateCommandLegality;
    }

    #endregion
}