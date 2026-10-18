using System.Reflection;

namespace Sora.Command;

/// <summary>
///     Framework implementation of <see cref="ICommandRegistrar" />. It is not an <see cref="AssemblyRegistration" />,
///     so plugins receiving it cannot cast it to obtain management operations.
/// </summary>
internal sealed class CommandRegistrar : ICommandRegistrar
{
    private readonly AssemblyRegistration _registration;

    /// <summary>
    ///     Creates a registrar bound to an assembly registration.
    /// </summary>
    /// <param name="registration">The owning assembly registration.</param>
    internal CommandRegistrar(AssemblyRegistration registration)
    {
        _registration = registration;
    }

    /// <inheritdoc />
    public Assembly Assembly => _registration.Assembly;

    /// <inheritdoc />
    public Guid RegisterDynamicCommand(
        Type                                  ownerType,
        Func<MessageReceivedEvent, ValueTask> handler,
        string[]                              expressions,
        MatchType                             matchType       = MatchType.Full,
        MessageSourceScope                    sourceScope     = MessageSourceScope.Any,
        MemberRole                            permissionLevel = MemberRole.Member,
        int                                   priority        = 0,
        bool                                  blockAfterMatch = true,
        string                                description     = "",
        bool                                  preventReentry  = true,
        string                                reentryMessage  = "",
        string                                prefix          = "",
        CommandBeforeFilterAttribute[]?       beforeFilters   = null,
        CommandAfterFilterAttribute[]?        afterFilters    = null,
        bool                                  superUserOnly   = false) =>
        _registration.Manager.RegisterDynamicCommand(
            _registration,
            ownerType,
            handler,
            expressions,
            matchType,
            sourceScope,
            permissionLevel,
            priority,
            blockAfterMatch,
            description,
            preventReentry,
            reentryMessage,
            prefix,
            beforeFilters,
            afterFilters,
            superUserOnly);

    /// <inheritdoc />
    public bool UnregisterDynamicCommand(Guid commandId) =>
        _registration.Manager.UnregisterDynamicCommand(_registration, commandId);
}