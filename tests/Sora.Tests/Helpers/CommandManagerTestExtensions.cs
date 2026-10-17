namespace Sora.Tests.Helpers;

/// <summary>
///     Shorthands for registering commands of the test assembly in unit and functional tests.
/// </summary>
internal static class CommandManagerTestExtensions
{
    /// <summary>
    ///     Gets the registration of the test assembly, registering it with default priority on first use.
    /// </summary>
    /// <param name="manager">The command manager.</param>
    /// <returns>The test assembly registration.</returns>
    public static AssemblyRegistration TestRegistration(this CommandManager manager) =>
        manager.TryGetAssembly(typeof(CommandManagerTestExtensions).Assembly, out AssemblyRegistration? registration)
            ? registration
            : manager.RegisterAssembly(typeof(CommandManagerTestExtensions).Assembly);

    /// <summary>
    ///     Gets the dynamic-command registrar of the test assembly registration.
    /// </summary>
    /// <param name="manager">The command manager.</param>
    /// <returns>The test assembly registrar.</returns>
    public static ICommandRegistrar TestRegistrar(this CommandManager manager) =>
        manager.TestRegistration().Registrar;
}
