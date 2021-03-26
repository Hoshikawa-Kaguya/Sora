using Sora.Attributes.Command;
using Sora.Entities.Info;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sora.Attributes;
using YukariToolBox.FormatLog;
using YukariToolBox.Helpers;

namespace Sora.Command
{
    /// <summary>
    /// 特性指令管理器
    /// </summary>
    public class CommandManager
    {
        #region 属性

        /// <summary>
        /// 指令服务正常运行标识
        /// </summary>
        public bool ServiceIsRunning { get; private set; }

        #endregion

        #region 私有字段

        private readonly List<CommandInfo> groupCommands = new();

        private readonly List<CommandInfo> privateCommands = new();

        private readonly Dictionary<Type, object> instanceDict = new();

        private readonly bool enableSoraCommandManager;

        #endregion

        #region 构造方法

        internal CommandManager(bool enableSoraCommandManager)
        {
            ServiceIsRunning              = false;
            this.enableSoraCommandManager = enableSoraCommandManager;
        }

        #endregion

        #region 公有管理方法

        /// <summary>
        /// 自动注册所有指令
        /// </summary>
        /// <param name="assembly">包含指令的程序集</param>
        public void MappingCommands(Assembly assembly)
        {
            //检查使能
            if (!enableSoraCommandManager) return;
            if (assembly == null) return;

            //查找所有的指令集
            var cmdGroups = assembly.GetExportedTypes()
                                    //获取指令组
                                    .Where(type => type.IsDefined(typeof(CommandGroup), false) && type.IsClass)
                                    .Select(type => (type,
                                                     type.GetMethods()
                                                         .Where(method => method.CheckMethodLegality())
                                                         .ToArray())
                                           )
                                    .ToDictionary(methods => methods.type, methods => methods.Item2.ToArray());

            //生成指令信息
            foreach (var (classType, methodInfos) in cmdGroups)
            {
                Dictionary<MethodInfo, AutoResetEvent> methodGroups = new Dictionary<MethodInfo, AutoResetEvent>();
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    methodGroups.Add(methodInfo, null);
                }

                MappingCommands(classType, methodGroups);
            }
        }

        public void MappingCommands(Type classType, Dictionary<MethodInfo, AutoResetEvent> cmdGroups)
        {
            foreach (var methodInfo in cmdGroups)
            {
                MappingCommands(classType, methodInfo.Key, false, methodInfo.Value);
            }

            //修改缓存大小
            Regex.CacheSize += privateCommands.Sum(commands => commands.Regex.Length) +
                               groupCommands.Sum(commands => commands.Regex.Length);

            Log.Info("Command", $"Registered {groupCommands.Count + privateCommands.Count} commands");
            ServiceIsRunning = true;
        }

        public void MappingCommands(Type classType, MethodInfo methodInfo, bool isUseSempaore, AutoResetEvent sempaore)
        {
            switch (GenerateCommandInfo(methodInfo, classType, isUseSempaore, sempaore,
                                        out var commandInfo))
            {
                case GroupCommand:
                    if (groupCommands.AddOrExist(commandInfo))
                        Log.Debug("Command", $"Registered group command [{methodInfo.Name}]");
                    break;
                case PrivateCommand:
                    if (privateCommands.AddOrExist(commandInfo))
                        Log.Debug("Command", $"Registered private command [{methodInfo.Name}]");
                    break;
                default:
                    Log.Warning("Command", "未知的指令类型");
                    break;
            }

            ServiceIsRunning = true;
        }

        public void UnMappingCommands(Type classType, MethodInfo methodInfo, AutoResetEvent sempaore)
        {
            switch (GenerateCommandInfo(methodInfo, classType, sempaore == null, sempaore,
                                        out var commandInfo))
            {
                case GroupCommand:
                    if (groupCommands.Remove(commandInfo))
                        Log.Debug("Command", $"Unregistered group command [{methodInfo.Name}]");
                    break;
                case PrivateCommand:
                    if (privateCommands.Remove(commandInfo))
                        Log.Debug("Command", $"Unregistered private command [{methodInfo.Name}]");
                    break;
                default:
                    Log.Warning("Command", "未知的指令类型");
                    break;
            }
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
            List<CommandInfo> matchedCommand;
            switch (eventArgs)
            {
                case GroupMessageEventArgs groupMessageEvent:
                {
                    //注意可能匹配到多个的情况，下同
                    matchedCommand =
                        groupCommands.Where(command => command.Regex.Any(regex =>
                                                                             Regex
                                                                                 .IsMatch(groupMessageEvent.Message.RawText,
                                                                                     regex)
                                                                          && command.MethodInfo != null))
                                     .OrderByDescending(p => p.Priority)
                                     .ToList();

                    break;
                }
                case PrivateMessageEventArgs privateMessageEvent:
                {
                    matchedCommand =
                        privateCommands.Where(command => command.Regex.Any(regex =>
                                                                               Regex
                                                                                   .IsMatch(privateMessageEvent.Message.RawText,
                                                                                       regex,
                                                                                       RegexOptions.Compiled)
                                                                            && command.MethodInfo != null))
                                       .OrderByDescending(p => p.Priority)
                                       .ToList();

                    break;
                }
                default:
                    Log.Error("CommandAdapter", "cannot parse eventArgs");
                    return true;
            }


            //在没有匹配到指令时直接跳转至Event触发
            if (matchedCommand.Count == 0) return true;

            //遍历匹配到的每个命令
            foreach (var commandInfo in matchedCommand)
            {
                //若是群，则判断权限
                if (eventArgs is GroupMessageEventArgs groupEventArgs)
                {
                    if (groupEventArgs.SenderInfo.Role < (commandInfo.PermissonType ?? MemberRoleType.Member))
                    {
                        Log.Warning("CommandAdapter",
                                    $"成员{groupEventArgs.SenderInfo.UserId}正在尝试执行指令{commandInfo.MethodInfo.Name}");

                        //权限不足，跳过本命令执行
                        continue;
                    }
                }

                Log.Debug("CommandAdapter",
                          $"trigger command [{commandInfo.MethodInfo.ReflectedType?.FullName}.{commandInfo.MethodInfo.Name}]");
                Log.Info("CommandAdapter", $"触发指令[{commandInfo.MethodInfo.Name}]");

                try
                {
                    //是否正在等待信号量，若是，则直接触发信号量即可
                    if (commandInfo.IsWaittingSemaphore)
                    {
                        commandInfo.Semaphore.Set();
                    }
                    else
                    {
                        var isAsyncMethod =
                            (AsyncStateMachineAttribute)
                            commandInfo.MethodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
                        //执行指令方法
                        if (isAsyncMethod)
                            await ((dynamic) commandInfo.MethodInfo
                                                        .Invoke(commandInfo.InstanceType == null ? null : instanceDict[commandInfo.InstanceType],
                                                                new[] {eventArgs}))!;
                        else
                            commandInfo.MethodInfo
                                       .Invoke(commandInfo.InstanceType == null ? null : instanceDict[commandInfo.InstanceType],
                                               new[] {eventArgs});
                    }

                    if (!((BaseSoraEventArgs) eventArgs).IsContinueEventChain) return false;
                }
                catch (Exception e)
                {
                    Log.Error("CommandAdapter", Log.ErrorLogBuilder(e));

                    //描述不为空，才需要打印错误信息
                    if (!string.IsNullOrEmpty(commandInfo.Desc))
                    {
                        switch (eventArgs)
                        {
                            case GroupMessageEventArgs groupMessageEvent:
                            {
                                await groupMessageEvent.Reply($"指令执行错误\n{commandInfo.Desc}");
                                break;
                            }
                            case PrivateMessageEventArgs privateMessageEvent:
                            {
                                await privateMessageEvent.Reply($"指令执行错误\n{commandInfo.Desc}");
                                break;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 获取已注册过的实例
        /// </summary>
        /// <param name="instance">实例</param>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>获取是否成功</returns>
        public bool GetInstance<T>(out T instance)
        {
            if (instanceDict.Any(type => type.Key == typeof(T)) && instanceDict[typeof(T)] is T outVal)
            {
                instance = outVal;
                return true;
            }

            instance = default;
            return false;
        }

        #endregion

        #region 私有管理方法

        private Attribute GenerateCommandInfo(MethodInfo     method,    Type classType, bool isWaittingSemaphore,
                                              AutoResetEvent semaphore, out CommandInfo commandInfo)
        {
            //获取指令属性
            var commandAttr =
                method.GetCustomAttribute(typeof(GroupCommand)) ??
                method.GetCustomAttribute(typeof(PrivateCommand)) ??
                throw new NullReferenceException("command attribute is null with unknown reason");

            Log.Debug("Command", $"Registering command [{method.Name}]");

            //处理指令匹配类型
            var match = (commandAttr as Attributes.Command.Command)?.MatchType ?? MatchType.Full;
            //处理表达式
            var matchExp = match switch
                           {
                               MatchType.Full => (commandAttr as Attributes.Command.Command)?.CommandExpressions
                                   .Select(command => $"^{command}$")
                                   .ToArray(),
                               MatchType.Regex => (commandAttr as Attributes.Command.Command)?.CommandExpressions,
                               MatchType.KeyWord => (commandAttr as Attributes.Command.Command)?.CommandExpressions
                                   .Select(command => $"[{command}]+")
                                   .ToArray(),
                               _ => null
                           };
            if (matchExp == null)
            {
                commandInfo = ObjectHelper.CreateInstance<CommandInfo>();
                return null;
            }

            //检查和创建实例
            if (!method.IsStatic && !CheckAndCreateInstance(classType))
            {
                commandInfo = ObjectHelper.CreateInstance<CommandInfo>();
                return null;
            }

            //创建指令信息
            commandInfo = new CommandInfo((commandAttr as Attributes.Command.Command)?.Description,
                                          matchExp,
                                          classType.Name,
                                          method,
                                          (commandAttr as GroupCommand)?.PermissionLevel,
                                          (commandAttr as Attributes.Command.Command)?.Priority ?? 0,
                                          isWaittingSemaphore,
                                          semaphore,
                                          method.IsStatic ? null : classType);

            return commandAttr;
        }


        /// <summary>
        /// 生成指令信息
        /// </summary>
        /// <summary>
        /// 检查实例的存在和生成
        /// </summary>
        [Reviewed("XiaoHe321", "2021-03-16 21:07")]
        private bool CheckAndCreateInstance(Type classType)
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
                var instance = classType.CreateInstance();

                //添加实例
                instanceDict.Add(classType ?? throw new ArgumentNullException(nameof(classType), "get null class type"),
                                 instance);
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