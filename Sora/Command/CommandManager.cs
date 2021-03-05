using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Command.Attributes;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace Sora.Command
{
    /// <summary>
    /// 特性指令管理器
    /// </summary>
    internal class CommandManager
    {
        #region 私有字段

        private readonly List<CommandInfo> groupCommands = new();

        private readonly List<CommandInfo> privateCommands = new();

        private readonly Dictionary<Type, object> instanceDict = new();

        #endregion


        #region 公有管理方法

        /// <summary>
        /// 自动注册所有指令
        /// </summary>
        /// <param name="assembly">包含指令的程序集</param>
        internal void MappingCommands(Assembly assembly)
        {
            if (assembly == null) return;
            //查找所有的指令集
            var types = assembly.GetExportedTypes()
                                .Where(type => type.GetCustomAttributes(true)
                                                   .Any(attr => attr is CommandGroup))
                                .ToArray();
            //遍历指令集注册指令
            foreach (var type in types)
            {
                //获取指令集信息
                var cmdGroupInfo = type.GetCustomAttribute<CommandGroup>();
                if (cmdGroupInfo == null)
                {
                    Log.Error("Command", "can not get commandgroup");
                    continue;
                }

                Log.Debug("Command", $"Registering command group {cmdGroupInfo.GroupName}");

                //遍历指令集内方法
                foreach (var methodInfo in type.GetMethods())
                {
                    //获取指令属性
                    var commandAttr = methodInfo.GetCustomAttribute(typeof(GroupCommand)) ??
                                      methodInfo.GetCustomAttribute(typeof(PrivateCommand));
                    if (commandAttr == null) continue;
                    //注册指令
                    //获取参数类型
                    var para = methodInfo.GetParameters();
                    //判断参数类型
                    if (para.Length != 1) continue;
                    if (para[0].ParameterType == typeof(GroupMessageEventArgs)   && commandAttr is PrivateCommand ||
                        para[0].ParameterType == typeof(PrivateMessageEventArgs) && commandAttr is GroupCommand)
                        continue;
                    if (commandAttr == null)
                        throw new NullReferenceException("command attribute is null with unknown reason");
                    Log.Debug("Command", $"Registering command [{methodInfo.Name}]");
                    //处理指令匹配类型
                    var match = (commandAttr as Attributes.Command)?.MatchType ?? MatchType.Full;
                    //处理表达式
                    var matchExp = match switch
                    {
                        MatchType.Full => $"^{(commandAttr as Attributes.Command)?.CommandExpression}$",
                        MatchType.Regex => (commandAttr as Attributes.Command)?.CommandExpression,
                        _ => null
                    };
                    if (matchExp == null) continue;
                    CommandInfo command;
                    if (!methodInfo.IsStatic)
                    {
                        //获取类属性
                        var classType = methodInfo.ReflectedType;
                        if (!classType?.IsClass ?? true)
                        {
                            Log.Error("Command", "method reflected objcet is not a class");
                            continue;
                        }

                        //检查是否已创建过实例
                        if (instanceDict.All(ins => ins.Key != classType))
                        {
                            //创建实例
                            var instance = FormatterServices.GetUninitializedObject(classType);
                            if (instance == null)
                            {
                                Log.Error("Command", $"can not create instance [{classType.FullName}]");
                                continue;
                            }

                            //添加实例
                            instanceDict.Add(classType, instance);
                        }

                        //在指令表中添加新的指令
                        command = new CommandInfo((commandAttr as Attributes.Command)?.Description,
                                                  matchExp,
                                                  cmdGroupInfo.GroupName,
                                                  methodInfo,
                                                  (commandAttr as GroupCommand)?.PermissionLevel,
                                                  (commandAttr as Attributes.Command)?.TriggerEventAfterCommand ??
                                                  false,
                                                  classType);
                    }
                    else
                    {
                        //在指令表中添加新的指令
                        command = new CommandInfo((commandAttr as Attributes.Command)?.Description,
                                                  matchExp,
                                                  cmdGroupInfo.GroupName,
                                                  methodInfo,
                                                  (commandAttr as GroupCommand)?.PermissionLevel,
                                                  (commandAttr as Attributes.Command)?.TriggerEventAfterCommand ??
                                                  false);
                    }

                    //添加指令
                    switch (commandAttr)
                    {
                        case GroupCommand:
                            groupCommands.Add(command);
                            Log.Debug("Command", $"Registered group command [{methodInfo.Name}]");
                            break;
                        case PrivateCommand:
                            privateCommands.Add(command);
                            Log.Debug("Command", $"Registered private command [{methodInfo.Name}]");
                            break;
                        default:
                            Log.Warning("Commadn", "未知的指令类型");
                            continue;
                    }
                }
            }

            Log.Info("Command", $"Registered {groupCommands.Count + privateCommands.Count} commands");
        }

        /// <summary>
        /// 处理聊天指令
        /// </summary>
        /// <param name="eventArgs">事件参数</param>
        internal ValueTask<bool> CommandAdapter(object eventArgs)
        {
            //处理消息段
            CommandInfo matchedCommand;
            switch (eventArgs)
            {
                case GroupMessageEventArgs groupEventArgs:
                {
                    matchedCommand =
                        groupCommands.SingleOrDefault(command => Regex.IsMatch(groupEventArgs.Message.RawText,
                                                                               command.Regex));
                    if (matchedCommand.MethodInfo == null) return new ValueTask<bool>(true);
                    //判断权限
                    if (groupEventArgs.SenderInfo.Role < (matchedCommand.PermissonType ?? MemberRoleType.Member))
                    {
                        Log.Warning("Command",
                                    $"成员{groupEventArgs.SenderInfo.UserId}正在尝试执行指令{matchedCommand.MethodInfo.Name}");
                        return new ValueTask<bool>(true);
                    }

                    break;
                }
                case PrivateMessageEventArgs privateEventArgs:
                {
                    matchedCommand =
                        privateCommands.SingleOrDefault(command => Regex.IsMatch(privateEventArgs.Message.RawText,
                                                            command.Regex));
                    if (matchedCommand.MethodInfo == null) return new ValueTask<bool>(true);
                    break;
                }
                default:
                    Log.Error("CommandAdapter", "cannot parse eventArgs");
                    return new ValueTask<bool>(true);
            }

            Log.Debug("CommandAdapter", $"get command {matchedCommand.MethodInfo.Name}");
            try
            {
                Log.Info("Command", $"Trigger command [{matchedCommand.MethodInfo.Name}]");
                //执行指令方法
                matchedCommand.MethodInfo.Invoke(instanceDict[matchedCommand.InstanceType], new[] {eventArgs});
            }
            catch (Exception e)
            {
                Log.Error("Command", Log.ErrorLogBuilder(e));
                if (!string.IsNullOrEmpty(matchedCommand.Desc))
                {
                    Log.Warning("Command Error", $"Command desc:{matchedCommand.Desc}");
                }
            }

            return new ValueTask<bool>(matchedCommand.TriggerEventAfterCommand);
        }

        #endregion
    }
}