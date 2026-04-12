namespace Sora;

/// <summary>
///     Factory for creating bot services. Protocol-agnostic — does not reference any adapter.
///     Each adapter provides its own convenience extension methods.
/// </summary>
public sealed class SoraServiceFactory
{
    /// <summary>Singleton instance for extension method usage.</summary>
    public static readonly SoraServiceFactory Instance = new();

    private SoraServiceFactory()
    {
    }

    /// <summary>
    ///     Creates a bot service from a pre-constructed adapter and config.
    ///     This is the protocol-agnostic entry point.
    /// </summary>
    /// <param name="adapter">A protocol adapter instance.</param>
    /// <param name="config">Service configuration.</param>
    /// <returns>A configured <see cref="SoraService" /> instance.</returns>
    public static SoraService CreateService(IBotAdapter adapter, IBotServiceConfig config) => new(adapter, config);
}