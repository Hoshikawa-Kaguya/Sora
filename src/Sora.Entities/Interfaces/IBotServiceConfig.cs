namespace Sora.Entities.Interfaces;

/// <summary>
///     Base configuration for a bot service.
/// </summary>
public interface IBotServiceConfig
{
    /// <summary>Actors marked IsSuperUser on events; group member permission checks still apply.</summary>
    UserId[] SuperUsers { get; }

    /// <summary>Actors whose events are dropped before automatic reads, waiters, filters and handlers.</summary>
    UserId[] BlockUsers { get; }

    /// <summary>Whether to enable the command manager.</summary>
    bool EnableCommandManager { get; }

    /// <summary>Whether to automatically mark messages as read after receiving them.</summary>
    bool AutoMarkMessageRead => true;

    /// <summary>Drop messages that self sent.</summary>
    bool DropSelfMessage => true;
}