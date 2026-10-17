using Sora.Command.Attributes;
using Sora.Core.Enums;
using Sora.Entities.Events;
using MatchType = Sora.Core.Enums.MatchType;

namespace Sora.Tests.CommandFixture;

/// <summary>
///     Fixture command group whose single command has a very high command priority. Tests pre-register an instance with
///     a recorder to observe execution order.
/// </summary>
[CommandGroup(Name = "fixture")]
public sealed class FixtureOrderCommands
{
    private readonly Action<string> _record;

    /// <summary>Creates the group with an execution recorder.</summary>
    /// <param name="record">Receives the command name when a command executes.</param>
    public FixtureOrderCommands(Action<string> record)
    {
        _record = record;
    }

    /// <summary>Records <c>B1</c>.</summary>
    /// <param name="e">The message event.</param>
    /// <returns>A completed task.</returns>
    [Command(Expressions = ["order"], MatchType = MatchType.Keyword, Priority = 1000, BlockAfterMatch = false)]
    public ValueTask B1(MessageReceivedEvent e)
    {
        _record("B1");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Fixture static command group.</summary>
[CommandGroup(Name = "fixture-static")]
public static class FixtureStaticCommands
{
    /// <summary>A static fixture command.</summary>
    /// <param name="e">The message event.</param>
    /// <returns>A completed task.</returns>
    [Command(Expressions = ["fixture-static"])]
    public static ValueTask Static(MessageReceivedEvent e) => ValueTask.CompletedTask;
}

/// <summary>
///     Abstract command base defined in the fixture assembly. Derived types in other assemblies inherit its command.
///     <see cref="Sora.Command.AssemblyRegistration.ScanCommandGroups" /> skips it because it cannot be instantiated.
/// </summary>
[CommandGroup(Name = "fixture-base")]
public abstract class FixtureCommandBase
{
    private readonly Action<string> _record;

    /// <summary>Creates the base with an execution recorder.</summary>
    /// <param name="record">Receives the command name when a command executes.</param>
    protected FixtureCommandBase(Action<string> record)
    {
        _record = record;
    }

    /// <summary>Records <c>Inherited</c>.</summary>
    /// <param name="e">The message event.</param>
    /// <returns>A completed task.</returns>
    [Command(Expressions = ["inherited"], MatchType = MatchType.Keyword, BlockAfterMatch = false)]
    public ValueTask Inherited(MessageReceivedEvent e)
    {
        _record("Inherited");
        return ValueTask.CompletedTask;
    }
}

/// <summary>Open generic command group; skipped by scanning because it cannot be instantiated.</summary>
/// <typeparam name="T">Unused type parameter.</typeparam>
[CommandGroup(Name = "fixture-generic")]
public sealed class FixtureGenericCommands<T>
{
    /// <summary>A command that is never registered.</summary>
    /// <param name="e">The message event.</param>
    /// <returns>A completed task.</returns>
    [Command(Expressions = ["fixture-generic"])]
    public static ValueTask Generic(MessageReceivedEvent e) => ValueTask.CompletedTask;
}
