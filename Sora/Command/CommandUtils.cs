using Sora.Command.Attributes;
using Sora.EventArgs.SoraEvent;
using System.Linq;
using System.Reflection;

namespace Sora.Command
{
    internal static class CommandUtils
    {
        #region 检查方法

        /// <summary>
        /// 检查方法合法性
        /// </summary>
        /// <param name="method">方法信息</param>
        internal static bool CheckMethodLegality(this MethodInfo method)
        {
            bool isGroupCommandLegality = method.IsDefined(typeof(GroupCommand), false) &&
                                          method.GetParameters().Length == 1            &&
                                          method.GetParameters()
                                                .Any(para =>
                                                         para.ParameterType == typeof(GroupMessageEventArgs) &&
                                                         !para.IsOut);

            bool isPrivateCommandLegality = method.IsDefined(typeof(PrivateCommand), false) &&
                                            method.GetParameters().Length == 1              &&
                                            method.GetParameters()
                                                  .Any(para =>
                                                           para.ParameterType == typeof(PrivateMessageEventArgs) &&
                                                           !para.IsOut);

            if (isGroupCommandLegality || isPrivateCommandLegality)
                return true;

            return false;
        }

        #endregion
    }
}