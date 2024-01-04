using Microsoft.Extensions.Hosting;

namespace Sora.Core;

public class SoraApp : IHost
{
    public IServiceProvider Services { get; }

    internal SoraApp(IHost host)
    {
        Services = host.Services;
    }

    public Task StartAsync(CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {

    }
}