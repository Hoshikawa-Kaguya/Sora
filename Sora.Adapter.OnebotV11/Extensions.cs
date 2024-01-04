using Sora.Core;
using Sora.Core.Network;

namespace Sora.Adapter.OnebotV11
{
    public static class Extensions
    {
        public static SoraAppBuilder UseOnebotV11Adapter(this SoraAppBuilder builder)
        {
            builder.AddScopedService<IMessageDispatcher, MessageDispatcher>();
            return builder;
        }
    }
}
