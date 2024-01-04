using Sora.Core.Enumeration;

namespace Sora.Core.Network;

public class SoraWebSocketServer : ISoraWebSocketService
{
    public object         SocketInstance { get; set; }
    public SoraSocketType SocketType     => SoraSocketType.Server;
    public void           Create(SoraConfig config)
    {
        throw new NotImplementedException();
    }

    public void           Start()
    {
        throw new NotImplementedException();
    }

    public void           Stop()
    {
        throw new NotImplementedException();
    }

    public void           SendMessage<T>(T message)
    {
        throw new NotImplementedException();
    }
}