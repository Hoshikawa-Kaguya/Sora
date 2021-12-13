using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sora.Attributes;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Info.InternalDataInfo;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Sora.Command;

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

    private readonly List<CommandInfo> _groupCommands = new();

    private readonly List<CommandInfo> _privateCommands = new();

    private readonly List<CommandInfo> _guildCommands = new();

    private readonly Dictionary<Type, dynamic> _instanceDict = new();

    #endregion

    #region 构造方法

    internal CommandManager(Assembly assembly)
    {
        ServiceIsRunning = false;
        MappingCommands(assembly);
    }

    #endregion

    #region 公有管理方法

    /// <summary>
    /// 自动注册所有指令
    /// </summary>
    /// <param name="assembly">包含指令的程序集</param>
    [Reviewed("XiaoHe321", "2021-03-28 20:45")]
    public void MappingCommands(Assembly assembly)
    {
        if (assembly == null) return;

        //查找所有的指令集
        var cmdGroups = assembly.GetExportedTypes()
                                //获取指令组
                                .Where(type => type.IsDefined(typeof(CommandGroup), false) && type.IsClass)
                                .Select(type => (type, type.GetMethods()
                                                           .Where(method => method.CheckMethodLegality())
                                                           .ToArray()))
                                .ToDictionary(methods => methods.type, methods => methods.Item2.ToArray());

        //生成指令信息
        foreach (var (classType, methodInfos) in cmdGroups)
        foreach (var methodInfo in methodInfos)
        {
            switch (GenerateCommandInfo(methodInfo, classType, out var commandInfo))
            {
                case GroupCommand:
                    if (_groupCommands.AddOrExist(commandInfo))
                        Log.Debug("CommandManager", $"Registered group command [{methodInfo.Name}]");
                    else
                        Log.Warning("CommandManager", "Command exists");
                    break;
                case PrivateCommand:
                    if (_privateCommands.AddOrExist(commandInfo))
                        Log.Debug("Command", $"Registered private command [{methodInfo.Name}]");
                    else
                        Log.Warning("CommandManager", "Command exists");
                    break;
                case GuildCommand:
                    if(_guildCommands.AddOrExist(commandInfo))
                        Log.Debug("Command", $"Registered guild command [{methodInfo.Name}]");
                    else
                        Log.Warning("CommandManager", "Command exists");
                    break;
                default:
                    Log.Warning("Command", "未知的指令类型");
                    break;
            }
        }

        //增加正则缓存大小
        Regex.CacheSize  += _privateCommands.Count + _groupCommands.Count + _guildCommands.Count;
        ServiceIsRunning =  true;
    }

    /// <summary>
    /// 动态创建指令
    /// </summary>
    /// <param name="exceptionHandler">异常处理</param>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <param name="permissionType">权限等级</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型时抛出</exception>
    [NeedReview("ALL")]
    public void RegisterGroupCommand(string[] cmdExps,
                                     Func<GroupMessageEventArgs, ValueTask> commandBlock,
                                     MatchType matchType,
                                     MemberRoleType permissionType = MemberRoleType.Member,
                                     RegexOptions regexOptions = RegexOptions.None,
                                     Action<Exception> exceptionHandler = null,
                                     string desc = "")
    {
        //生成指令信息
        if (_groupCommands.AddOrExist(GenDynamicCommandInfo(desc, cmdExps, matchType, regexOptions, commandBlock,
                                                            permissionType, exceptionHandler)))
            Log.Debug("Command", "Registered group command [dynamic]");
        else
            throw new NotSupportedException("cannot add new group command");

        //增加正则缓存大小
        Regex.CacheSize += 1;
    }

    //TODO 不知道gocq什么时候能从message获取成员权限
    /// <summary>
    /// 动态创建指令
    /// </summary>
    /// <param name="exceptionHandler">异常处理</param>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型时抛出</exception>
    public void RegisterGuildCommand(string[] cmdExps,
                                     Func<GuildMessageEventArgs, ValueTask> commandBlock,
                                     MatchType matchType,
                                     //MemberRoleType permissionType = MemberRoleType.Member,
                                     RegexOptions regexOptions = RegexOptions.None,
                                     Action<Exception> exceptionHandler = null,
                                     string desc = "")
    {
        //生成指令信息
        if (_guildCommands.AddOrExist(GenDynamicCommandInfo(desc, cmdExps, matchType, regexOptions, commandBlock,
                                                            MemberRoleType.Member, exceptionHandler)))
            Log.Debug("Command", "Registered group command [dynamic]");
        else
            throw new NotSupportedException("cannot add new group command");

        //增加正则缓存大小
        Regex.CacheSize += 1;
    }

    /// <summary>
    /// 动态创建指令
    /// </summary>
    /// <param name="exceptionHandler">异常处理</param>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <param name="permissionType">权限等级</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型时抛出</exception>
    [NeedReview("ALL")]
    public void RegisterPrivateCommand(string[] cmdExps,
                                       Func<PrivateMessageEventArgs, ValueTask> commandBlock,
                                       MatchType matchType,
                                       MemberRoleType permissionType = MemberRoleType.Member,
                                       RegexOptions regexOptions = RegexOptions.None,
                                       Action<Exception> exceptionHandler = null,
                                       string desc = "")
    {
        //生成指令信息
        if (_privateCommands.AddOrExist(GenDynamicCommandInfo(desc, cmdExps, matchType, regexOptions, commandBlock,
                                                              permissionType, exceptionHandler)))
            Log.Debug("Command", "Registered private command [dynamic]");
        else
            throw new NotSupportedException("cannot add new group command");

        //增加正则缓存大小
        Regex.CacheSize += 1;
    }

    /// <summary>
    /// 处理聊天指令的单次处理器
    /// </summary>
    /// <param name="eventArgs">事件参数</param>
    /// <returns>是否继续处理接下来的消息</returns>
    [NeedReview("L317-L374")]
    internal async ValueTask CommandAdapter(BaseSoraEventArgs eventArgs)
    {
        #region 信号量消息处理

        //处理消息段
        Dictionary<Guid, CommandWaitingInfo> waitingCommand;
        switch (eventArgs)
        {
            case GroupMessageEventArgs groupMessageEvent:
            {
                //注意可能匹配到多个的情况，下同
                waitingCommand = StaticVariable.WaitingDict
                                               .Where(command =>
                                                          IsWaitingCommand(SourceFlag.Group, command.Value,
                                                                           groupMessageEvent,
                                                                           groupMessageEvent.Messages))
                                               .ToDictionary(i => i.Key, i => i.Value);
                break;
            }
            case PrivateMessageEventArgs privateMessageEvent:
            {
                waitingCommand = StaticVariable.WaitingDict
                                               .Where(command =>
                                                          IsWaitingCommand(SourceFlag.Private, command.Value,
                                                                           privateMessageEvent,
                                                                           privateMessageEvent.Messages))
                                               .ToDictionary(i => i.Key, i => i.Value);
                break;
            }
            case GuildMessageEventArgs guildMessageEvent:
            {
                waitingCommand = StaticVariable.WaitingDict
                                               .Where(command =>
                                                          IsWaitingCommand(SourceFlag.Guild, command.Value,
                                                                           guildMessageEvent,
                                                                           guildMessageEvent.Messages))
                                               .ToDictionary(i => i.Key, i => i.Value);
                break;
            }
            default:
                Log.Error("CommandAdapter", "cannot parse eventArgs");
                return;
        }

        foreach (var (key, _) in waitingCommand)
        {
            //更新等待列表，设置为当前的eventArgs
            var oldInfo = StaticVariable.WaitingDict[key];
            var newInfo = oldInfo;
            newInfo.EventArgs = eventArgs;
            StaticVariable.WaitingDict.TryUpdate(key, newInfo, oldInfo);
            StaticVariable.WaitingDict[key].Semaphore.Set();
        }

        //当前流程已经处理过wait command了。不再继续处理普通command，否则会一次发两条消息，普通消息留到下一次处理
        if (waitingCommand.Count != 0)
        {
            eventArgs.IsContinueEventChain = false;
            return;
        }

        #endregion

        #region 常规指令处理

        //检查指令池
        if (_groupCommands.Count == 0 && _privateCommands.Count == 0 && _guildCommands.Count== 0) return;

        List<CommandInfo> matchedCommand =
            eventArgs switch
            {
                GroupMessageEventArgs groupMessageEvent =>
                    _groupCommands.Where(command => IsMatchedCommand(groupMessageEvent.Messages, command))
                                  .OrderByDescending(p => p.Priority)
                                  .ToList(),
                PrivateMessageEventArgs privateMessageEvent =>
                    _privateCommands.Where(command => IsMatchedCommand(privateMessageEvent.Messages, command))
                                    .OrderByDescending(p => p.Priority)
                                    .ToList(),
                GuildMessageEventArgs guildMessageEvent => 
                    _guildCommands.Where(command => IsMatchedCommand(guildMessageEvent.Messages, command))
                                    .OrderByDescending(p => p.Priority)
                                    .ToList(),
                _ => null
            };
        if (matchedCommand is null)
        {
            Log.Error("CommandAdapter", "cannot parse eventArgs");
            return;
        }

        //在没有匹配到指令时直接跳转至Event触发
        if (matchedCommand.Count == 0) return;
        await InvokeMatchCommand(matchedCommand, eventArgs);

        #endregion
    }

    /// <summary>
    /// 获取已注册过的实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>获取是否成功</returns>
    [Reviewed("XiaoHe321", "2021-03-28 20:45")]
    public bool GetInstance<T>(out T instance)
    {
        if (_instanceDict.Any(type => type.Key == typeof(T)) && _instanceDict[typeof(T)] is T outVal)
        {
            instance = outVal;
            return true;
        }

        instance = default;
        return false;
    }

    #endregion

    #region 指令匹配判断

    /// <summary>
    /// 指令是否为连续对话并在等待
    /// </summary>
    /// <param name="message"></param>
    /// <param name="source"></param>
    /// <param name="waitingInfo"></param>
    /// <param name="eventArgs"></param>
    private static bool IsWaitingCommand(SourceFlag source, CommandWaitingInfo waitingInfo,
                                         BaseSoraEventArgs eventArgs,
                                         Message message)
    {
        return source switch
        {
            SourceFlag.Group =>
                waitingInfo.SourceFlag == SourceFlag.Group
                //判断来自同一个连接
             && waitingInfo.ConnectionId == eventArgs.SoraApi.ConnectionId
                //判断来着同一个群
             && waitingInfo.Source.GroupId == eventArgs.Source.GroupId
                //判断来自同一人
             && waitingInfo.Source.UserId == eventArgs.Source.UserId
                //匹配
             && IsMatchedCommand(message, waitingInfo),
            SourceFlag.Private =>
                waitingInfo.SourceFlag == SourceFlag.Private
                //判断来自同一个连接
             && waitingInfo.ConnectionId == eventArgs.SoraApi.ConnectionId
                //判断来自同一人
             && waitingInfo.Source.UserId == eventArgs.Source.UserId
                //匹配
             && IsMatchedCommand(message, waitingInfo),
            SourceFlag.Guild =>
                waitingInfo.SourceFlag == SourceFlag.Guild
                //判断来自同一个连接
             && waitingInfo.ConnectionId == eventArgs.SoraApi.ConnectionId
                //判断来着同一个频道
             && waitingInfo.Source.GuildId == eventArgs.Source.GuildId
                //判断来自同一个子频道
             && waitingInfo.Source.ChannelId == eventArgs.Source.ChannelId
                //判断来自同一个人
             && waitingInfo.Source.UserGuildId == eventArgs.Source.UserGuildId
                //匹配
             && IsMatchedCommand(message, waitingInfo),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }

    /// <summary>
    /// 匹配指令正则(连续对话指令)
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="command">指令信息</param>
    private static bool IsMatchedCommand(Message message, CommandWaitingInfo command)
    {
        return command.CommandExpressions.Any(regex =>
                                                  Regex.IsMatch(message.RawText,
                                                                regex,
                                                                RegexOptions.Compiled |
                                                                command.RegexOptions));
    }

    /// <summary>
    /// 匹配指令正则(普通指令)
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="command">指令信息</param>
    private static bool IsMatchedCommand(Message message, CommandInfo command)
    {
        return command.Regex.Any(regex =>
                                     Regex.IsMatch(message.RawText,
                                                   regex,
                                                   RegexOptions.Compiled |
                                                   command.RegexOptions));
    }

    #endregion

    #region 私有管理方法

    /// <summary>
    /// 生成连续对话上下文
    /// </summary>
    /// <param name="source">消息源</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="sourceFlag">来源标识</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="connectionId">连接标识</param>
    /// <param name="serviceId">服务标识</param>
    /// <exception cref="NullReferenceException">表达式为空时抛出异常</exception>
    internal static CommandWaitingInfo GenWaitingCommandInfo(
        EventSource source, string[] cmdExps, MatchType matchType, SourceFlag sourceFlag,
        RegexOptions regexOptions, Guid connectionId, Guid serviceId)
    {
        if (cmdExps == null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        var matchExp = matchType switch
        {
            MatchType.Full => cmdExps
                              .Select(command => $"^{command}$")
                              .ToArray(),
            MatchType.Regex => cmdExps,
            MatchType.KeyWord => cmdExps
                                 .Select(command => $"[{command}]+")
                                 .ToArray(),
            _ => throw new NotSupportedException("unknown matchtype")
        };

        return new CommandWaitingInfo(new AutoResetEvent(false),
                                      matchExp,
                                      serviceId: serviceId,
                                      connectionId: connectionId,
                                      source: source,
                                      sourceFlag: sourceFlag,
                                      regexOptions: regexOptions);
    }

    /// <summary>
    /// 生成指令信息
    /// </summary>
    /// <param name="method">指令method</param>
    /// <param name="classType">所在实例类型</param>
    /// <param name="commandInfo">指令信息</param>
    private Attribute GenerateCommandInfo(MethodInfo method, Type classType, out CommandInfo commandInfo)
    {
        //获取指令属性
        var commandAttr =
            method.GetCustomAttribute(typeof(GroupCommand)) ??
            method.GetCustomAttribute(typeof(GuildCommand)) ??
            method.GetCustomAttribute(typeof(PrivateCommand)) ??
            throw new NullReferenceException("command attribute is null with unknown reason");

        Log.Debug("Command", $"Registering command [{method.Name}]");

        //处理指令匹配类型
        var match = (commandAttr as RegexCommand)?.MatchType ?? MatchType.Full;
        //处理表达式
        var matchExp = ParseCommandExps((commandAttr as RegexCommand)?.CommandExpressions, match);
        //若无匹配表达式，则创建一个空白的命令信息
        if (matchExp == null)
        {
            commandInfo = Helper.CreateInstance<CommandInfo>();
            return null;
        }

        //检查和创建实例
        //若创建实例失败且方法不是静态的，则返回空白命令信息
        if (!method.IsStatic && !CheckAndCreateInstance(classType))
        {
            commandInfo = Helper.CreateInstance<CommandInfo>();
            return null;
        }

        //创建指令信息
        commandInfo = new CommandInfo((commandAttr as RegexCommand)?.Description,
                                      matchExp,
                                      classType.Name,
                                      method,
                                      (commandAttr as GroupCommand)?.PermissionLevel ?? MemberRoleType.Member,
                                      (commandAttr as RegexCommand)?.Priority        ?? 0,
                                      (commandAttr as RegexCommand)?.RegexOptions ??
                                      RegexOptions.None,
                                      (commandAttr as RegexCommand)?.ExceptionHandler,
                                      method.IsStatic ? null : classType);

        return commandAttr;
    }

    /// <summary>
    /// 生成动态指令信息(群聊)
    /// </summary>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <param name="permissionType">权限等级</param>
    /// <param name="exceptionHandler">异常处理</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型是抛出</exception>
    [NeedReview("ALL")]
    private CommandInfo GenDynamicCommandInfo(string desc, string[] cmdExps, MatchType matchType,
                                              RegexOptions regexOptions,
                                              Func<GroupMessageEventArgs, ValueTask> commandBlock,
                                              MemberRoleType permissionType, Action<Exception> exceptionHandler)
    {
        //判断参数合法性
        if (cmdExps == null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        var parameters = commandBlock.Method.GetParameters();
        if (parameters.Length != 1) throw new NotSupportedException("unsupport parameter count");
        //处理表达式
        var matchExp = ParseCommandExps(cmdExps, matchType);

        //判断指令响应源
        if (parameters.First().ParameterType != typeof(GroupMessageEventArgs))
            throw new NotSupportedException("unsupport parameter type");

        var priority = _groupCommands.Count == 0 ? 0 : _groupCommands.Min(cmd => cmd.Priority) - 1;

        //创建指令信息
        return new CommandInfo(desc, matchExp, "dynamic", commandBlock, exceptionHandler, permissionType,
                               priority, regexOptions | RegexOptions.Compiled);
    }

    /// <summary>
    /// 生成动态指令信息(私聊)
    /// </summary>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <param name="permissionType">权限等级</param>
    /// <param name="exceptionHandler">异常处理</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型是抛出</exception>
    [NeedReview("ALL")]
    private CommandInfo GenDynamicCommandInfo(string desc, string[] cmdExps, MatchType matchType,
                                              RegexOptions regexOptions,
                                              Func<PrivateMessageEventArgs, ValueTask> commandBlock,
                                              MemberRoleType permissionType, Action<Exception> exceptionHandler)
    {
        //判断参数合法性
        if (cmdExps == null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        var parameters = commandBlock.Method.GetParameters();
        if (parameters.Length != 1) throw new NotSupportedException("unsupport parameter count");
        //处理表达式
        var matchExp = ParseCommandExps(cmdExps, matchType);

        //判断指令响应源
        if (parameters.First().ParameterType != typeof(GroupMessageEventArgs))
            throw new NotSupportedException("unsupport parameter type");

        var priority = _privateCommands.Count == 0 ? 0 : _privateCommands.Min(cmd => cmd.Priority) - 1;

        //创建指令信息
        return new CommandInfo(desc, matchExp, "dynamic", commandBlock, exceptionHandler, permissionType,
                               priority, regexOptions | RegexOptions.Compiled);
    }

    /// <summary>
    /// 生成动态指令信息(私聊)
    /// </summary>
    /// <param name="desc">指令描述</param>
    /// <param name="cmdExps">指令表达式</param>
    /// <param name="matchType">匹配类型</param>
    /// <param name="regexOptions">正则匹配选项</param>
    /// <param name="commandBlock">指令委托</param>
    /// <param name="permissionType">权限等级</param>
    /// <param name="exceptionHandler">异常处理</param>
    /// <exception cref="NullReferenceException">空参数异常</exception>
    /// <exception cref="NotSupportedException">在遇到不支持的参数类型是抛出</exception>
    [NeedReview("ALL")]
    private CommandInfo GenDynamicCommandInfo(string desc, string[] cmdExps, MatchType matchType,
                                              RegexOptions regexOptions,
                                              Func<GuildMessageEventArgs, ValueTask> commandBlock,
                                              MemberRoleType permissionType, Action<Exception> exceptionHandler)
    {
        //判断参数合法性
        if (cmdExps == null || cmdExps.Length == 0) throw new NullReferenceException("cmdExps is empty");
        var parameters = commandBlock.Method.GetParameters();
        if (parameters.Length != 1) throw new NotSupportedException("unsupport parameter count");
        //处理表达式
        var matchExp = ParseCommandExps(cmdExps, matchType);

        //判断指令响应源
        if (parameters.First().ParameterType != typeof(GuildMessageEventArgs))
            throw new NotSupportedException("unsupport parameter type");

        var priority = _guildCommands.Count == 0 ? 0 : _guildCommands.Min(cmd => cmd.Priority) - 1;

        //创建指令信息
        return new CommandInfo(desc, matchExp, "dynamic", commandBlock, exceptionHandler, permissionType,
                               priority, regexOptions | RegexOptions.Compiled);
    }

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
        if (_instanceDict.Any(ins => ins.Key == classType)) return true;

        try
        {
            //创建实例
            var instance = classType.CreateInstance();

            //添加实例
            _instanceDict
                .Add(classType ?? throw new ArgumentNullException(nameof(classType), "get null class type"),
                     instance);
        }
        catch (Exception e)
        {
            Log.Error("Command", $"cannot create instance with error:{Log.ErrorLogBuilder(e)}");
            return false;
        }

        return true;
    }

    private async ValueTask InvokeMatchCommand(List<CommandInfo> matchedCommands, BaseSoraEventArgs eventArgs)
    {
        //遍历匹配到的每个命令
        foreach (var commandInfo in matchedCommands)
        {
            //若是群，则判断权限
            if (eventArgs is GroupMessageEventArgs groupEventArgs)
                if (groupEventArgs.SenderInfo.Role < commandInfo.PermissionType)
                {
                    Log.Warning("CommandAdapter",
                                $"成员{groupEventArgs.SenderInfo.UserId}正在尝试执行指令{commandInfo.MethodInfo.Name}");

                    //权限不足，跳过本命令执行
                    continue;
                }

            try
            {
                //判断不同的执行方法
                switch (commandInfo.InvokeType)
                {
                    //特性指令
                    case InvokeType.Method:
                    {
                        Log.Debug("CommandAdapter",
                                  $"trigger command [{commandInfo.MethodInfo.ReflectedType?.FullName}.{commandInfo.MethodInfo.Name}]");
                        Log.Info("CommandAdapter", $"触发指令[{commandInfo.MethodInfo.Name}]");
                        //尝试执行指令并判断异步方法
                        var isAsnyc =
                            commandInfo.MethodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute),
                                                                      false) is not null;
                        //执行指令方法
                        if (isAsnyc && commandInfo.MethodInfo.ReturnType != typeof(void))
                        {
                            Log.Debug("Command", "invoke async command method");
                            await commandInfo.MethodInfo
                                             .Invoke(commandInfo.InstanceType == null ? null : _instanceDict[commandInfo.InstanceType],
                                                     new object[] {eventArgs});
                        }
                        else
                        {
                            Log.Debug("Command", "invoke command method");
                            commandInfo.MethodInfo
                                       .Invoke(commandInfo.InstanceType == null ? null : _instanceDict[commandInfo.InstanceType],
                                               new object[] {eventArgs});
                        }

                        break;
                    }
                    //动态注册指令
                    case InvokeType.Action:
                    {
                        Log.Debug("CommandAdapter",
                                  $"trigger command [dynamic command({commandInfo.Priority})]");
                        Log.Info("CommandAdapter", $"触发指令[dynamic command({commandInfo.Priority})]");
                        switch (commandInfo.SourceFlag)
                        {
                            case SourceFlag.Group:
                                await commandInfo.GroupActionBlock.Invoke(eventArgs as GroupMessageEventArgs);
                                break;
                            case SourceFlag.Private:
                                await commandInfo.PrivateActionBlock.Invoke(eventArgs as PrivateMessageEventArgs);
                                break;
                            case SourceFlag.Guild:
                                await commandInfo.GuildActionBlock.Invoke(eventArgs as GuildMessageEventArgs);
                                break;
                        }

                        break;
                    }
                    default:
                        Log.Error("CommandAdapter", "Get unknown command type");
                        break;
                }

                return;
            }
            catch (Exception e)
            {
                var errLog = Log.ErrorLogBuilder(e);
                Log.Error("CommandAdapter", errLog);

                var msg = new StringBuilder();
                msg.AppendLine("指令执行错误");
                if (!string.IsNullOrEmpty(commandInfo.Desc))
                    msg.AppendLine($"Description：{commandInfo.Desc}");
                msg.Append(Log.ErrorLogBuilder(e));


                switch (eventArgs)
                {
                    case GroupMessageEventArgs groupMessageArgs:
                    {
                        await groupMessageArgs.Reply(msg.ToString()).RunCatch(err => throw err);
                        break;
                    }
                    case PrivateMessageEventArgs privateMessageArgs:
                    {
                        await privateMessageArgs.Reply(msg.ToString()).RunCatch(err => throw err);
                        break;
                    }
                }

                //检查是否有异常处理
                if (commandInfo.ExceptionHandler is not null)
                    commandInfo.ExceptionHandler(e);
                else throw;
            }
        }
    }

    #endregion

    #region 指令表达式处理

    /// <summary>
    /// 处理指令正则表达式
    /// </summary>
    [NeedReview("ALL")]
    private static string[] ParseCommandExps(string[] cmdExps, MatchType matchType)
    {
        return matchType switch
        {
            MatchType.Full => cmdExps
                              .Select(command => $"^{command}$")
                              .ToArray(),
            MatchType.Regex => cmdExps,
            MatchType.KeyWord => cmdExps
                                 .Select(command => $"[{command}]+")
                                 .ToArray(),
            _ => throw new NotSupportedException("unknown matchtype")
        };
    }

    #endregion
}