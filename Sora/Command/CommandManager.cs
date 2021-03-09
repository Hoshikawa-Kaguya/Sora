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

        private readonly bool enableSoraCommandManager;

        #endregion

        #region 构造方法

        internal CommandManager(bool enableSoraCommandManager)
        {
            this.enableSoraCommandManager = enableSoraCommandManager;
        }

        #endregion

        #region 公有管理方法

        /// <summary>
        /// 自动注册所有指令
        /// </summary>
        /// <param name="assembly">包含指令的程序集</param>
        internal void MappingCommands(Assembly assembly)
        {
            //检查使能
            if (!enableSoraCommandManager) return;
            if (assembly == null) return;
            //查找所有的指令集
            var cmdGroups = assembly.GetExportedTypes()
                                    //获取指令组
                                    .Where(type => type.IsDefined(typeof(CommandGroup), false) && type.IsClass)
                                    .Select(type => (type, type.GetMethods()
                                                               .Where(method => method.CheckMethod())
                                                               .ToArray())
                                           )
                                    .ToDictionary(methods => methods.type, methods => methods.Item2.ToArray());

            //生成指令信息
            foreach (var (classType, methodInfos) in cmdGroups)
            {
                foreach (var methodInfo in methodInfos)
                {
                    switch (GenerateCommandInfo(methodInfo, classType, out CommandInfo commandInfo))
                    {
                        case GroupCommand:
                            groupCommands.Add(commandInfo);
                            Log.Debug("Command", $"Registered group command [{methodInfo.Name}]");
                            break;
                        case PrivateCommand:
                            privateCommands.Add(commandInfo);
                            Log.Debug("Command", $"Registered private command [{methodInfo.Name}]");
                            break;
                        default:
                            Log.Warning("Command", "未知的指令类型");
                            continue;
                    }
                }
            }

            //修改缓存大小
            Regex.CacheSize += privateCommands.Sum(commands => commands.Regex.Length) +
                               groupCommands.Sum(commands => commands.Regex.Length);
            Log.Info("Command", $"Registered {groupCommands.Count + privateCommands.Count} commands");
        }

        /// <summary>
        /// 处理聊天指令
        /// </summary>
        /// <param name="eventArgs">事件参数</param>
        internal async ValueTask<bool> CommandAdapter(object eventArgs)
        {
            //检查使能
            if (!enableSoraCommandManager) return true;
            //处理消息段
            CommandInfo matchedCommand;
            switch (eventArgs)
            {
                case GroupMessageEventArgs groupMessageEvent:
                {
                    matchedCommand =
                        groupCommands.SingleOrDefault(command => command.Regex.Any(regex =>
                                                          Regex.IsMatch(groupMessageEvent.Message.RawText,
                                                                        regex)));
                    if (matchedCommand.MethodInfo == null) return true;
                    //判断权限
                    if (groupMessageEvent.SenderInfo.Role < (matchedCommand.PermissonType ?? MemberRoleType.Member))
                    {
                        Log.Warning("CommandAdapter",
                                    $"成员{groupMessageEvent.SenderInfo.UserId}正在尝试执行指令{matchedCommand.MethodInfo.Name}");
                        return true;
                    }

                    break;
                }
                case PrivateMessageEventArgs privateMessageEvent:
                {
                    matchedCommand =
                        privateCommands.SingleOrDefault(command => command.Regex.Any(regex =>
                                                            Regex
                                                                .IsMatch(privateMessageEvent.Message.RawText,
                                                                         regex, RegexOptions.Compiled)));
                    if (matchedCommand.MethodInfo == null) return true;
                    break;
                }
                default:
                    Log.Error("CommandAdapter", "cannot parse eventArgs");
                    return true;
            }

            Log.Debug("CommandAdapter", $"get command {matchedCommand.MethodInfo.Name}");
            try
            {
                Log.Info("CommandAdapter", $"trigger command [{matchedCommand.MethodInfo.Name}]");
                //执行指令方法
                matchedCommand.MethodInfo.Invoke(instanceDict[matchedCommand.InstanceType], new[] {eventArgs});
            }
            catch (Exception e)
            {
                Log.Error("CommandAdapter", Log.ErrorLogBuilder(e));
                if (string.IsNullOrEmpty(matchedCommand.Desc)) return matchedCommand.TriggerEventAfterCommand;
                switch (eventArgs)
                {
                    case GroupMessageEventArgs groupMessageEvent:
                    {
                        await groupMessageEvent.Reply($"指令执行错误\n指令信息:{matchedCommand.Desc}");
                        break;
                    }
                    case PrivateMessageEventArgs privateMessageEvent:
                    {
                        await privateMessageEvent.Reply($"指令执行错误\n指令信息:{matchedCommand.Desc}");
                        break;
                    }
                }
            }

            return matchedCommand.TriggerEventAfterCommand;
        }

        #endregion

        #region 私有管理方法

        /// <summary>
        /// 生成指令信息
        /// </summary>
        private Attribute GenerateCommandInfo(MethodInfo method, Type classType, out CommandInfo commandInfo)
        {
            //获取指令属性
            var commandAttr = method.GetCustomAttribute(typeof(GroupCommand)) ??
                              method.GetCustomAttribute(typeof(PrivateCommand));
            if (commandAttr == null)
                throw new NullReferenceException("command attribute is null with unknown reason");
            Log.Debug("Command", $"Registering command [{method.Name}]");
            //处理指令匹配类型
            var match = (commandAttr as Attributes.Command)?.MatchType ?? MatchType.Full;
            //处理表达式
            var matchExp = match switch
            {
                MatchType.Full => (commandAttr as Attributes.Command)?.CommandExpressions
                                                                     .Select(command => $"^{command}$").ToArray(),
                MatchType.Regex => (commandAttr as Attributes.Command)?.CommandExpressions,
                _ => null
            };
            if (matchExp == null)
            {
                commandInfo = (CommandInfo) FormatterServices.GetUninitializedObject(typeof(CommandInfo));
                return null;
            }

            //检查和创建实例
            if (!method.IsStatic && !CheckAndCreateInstace(classType))
            {
                commandInfo = (CommandInfo) FormatterServices.GetUninitializedObject(typeof(CommandInfo));
                return null;
            }

            //创建指令信息
            commandInfo = new CommandInfo((commandAttr as Attributes.Command).Description,
                                          matchExp,
                                          classType.Name,
                                          method,
                                          (commandAttr as GroupCommand)?.PermissionLevel,
                                          (Attributes.Command) commandAttr is {TriggerEventAfterCommand: true},
                                          method.IsStatic ? null : classType);

            return commandAttr;
        }

        /// <summary>
        /// 检查实例的存在和生成
        /// </summary>
        private bool CheckAndCreateInstace(Type classType)
        {
            //获取类属性
            if (!classType?.IsClass ?? true)
            {
                Log.Error("Command", "method reflected objcet is not a class");
                return false;
            }

            //检查是否已创建过实例
            if (instanceDict.Any(ins => ins.Key == classType)) return true;

            try
            {
                //创建实例
                var instance = FormatterServices.GetUninitializedObject(classType);

                //添加实例
                instanceDict.Add(classType, instance);
            }
            catch (Exception e)
            {
                Log.Error("Command", $"cannot create instance with error:{Log.ErrorLogBuilder(e)}");
                return false;
            }

            return true;
        }

        #endregion
    }
}