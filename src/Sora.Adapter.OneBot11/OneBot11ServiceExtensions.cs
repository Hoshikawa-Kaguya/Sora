namespace Sora.Adapter.OneBot11;

/// <summary>
///     Extension methods for creating OneBot v11 bot services.
/// </summary>
public static class OneBot11ServiceExtensions
{
    /// <summary>
    ///     Creates a bot service with the OneBot v11 adapter.
    /// </summary>
    /// <param name="_">The service factory instance.</param>
    /// <param name="config">OneBot v11 configuration.</param>
    /// <returns>A configured <see cref="SoraService" /> instance.</returns>
    public static SoraService CreateOneBot11Service(this SoraServiceFactory _, OneBot11Config config)
    {
        OneBot11Adapter adapter = new(config);
        return SoraServiceFactory.CreateService(adapter, config);
    }
}