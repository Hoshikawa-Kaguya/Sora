namespace Sora.Adapter.Milky;

/// <summary>
///     Extension methods for creating Milky bot services.
/// </summary>
public static class MilkyServiceExtensions
{
    /// <summary>
    ///     Creates a bot service with the Milky adapter.
    /// </summary>
    /// <param name="_">The service factory instance.</param>
    /// <param name="config">Milky configuration.</param>
    /// <returns>A configured <see cref="SoraService" /> instance.</returns>
    public static SoraService CreateMilkyService(this SoraServiceFactory _, MilkyConfig config)
    {
        MilkyAdapter adapter = new(config);
        return SoraServiceFactory.CreateService(adapter, config);
    }
}