using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sora.Util;

internal static class ServiceHelper
{
    public static readonly IServiceCollection Services = new ServiceCollection();

    public static IServiceScope CreateScope()
    {
        return Services.BuildServiceProvider().CreateScope();
    }

    public static T GetService<T>()
    {
        return Services.BuildServiceProvider().GetService<T>();
    }

    public static dynamic GetService(Type type)
    {
        return Services.BuildServiceProvider().GetService(type);
    }
}