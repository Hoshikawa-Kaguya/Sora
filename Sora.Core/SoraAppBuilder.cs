using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sora.Core;

public class SoraAppBuilder
{
    private readonly HostApplicationBuilder _hostAppBuilder;

    private IServiceCollection _services => _hostAppBuilder.Services;

    public SoraAppBuilder()
    {
        _hostAppBuilder = new HostApplicationBuilder();
    }

    public SoraAppBuilder UseWebSocketServer(SoraConfig config)
    {
        //TODO
        return this;
    }

    public SoraApp Build() => new(_hostAppBuilder.Build());

    public void AddScopedService<TService, TIpml>() where TService : class where TIpml : class, TService
    {
        _services.AddScoped<TService, TIpml>();
    }
}