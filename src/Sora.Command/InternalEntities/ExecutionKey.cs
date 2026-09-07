using System.Reflection;

namespace Sora.Command.InternalEntities;

/// <summary>
///     Identifies a unique in-flight command execution for re-entry protection.
/// </summary>
internal readonly record struct ExecutionKey(
    MethodInfo        Method,
    Guid              ConnectionId,
    UserId            SenderId,
    GroupId           GroupId,
    MessageSourceType SourceType);