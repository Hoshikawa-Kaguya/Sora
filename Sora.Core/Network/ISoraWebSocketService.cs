using Sora.Core.Enumeration;

namespace Sora.Core.Network;

public interface ISoraWebSocketService
{
    object SocketInstance { get; set; }

    SoraSocketType SocketType { get; }

    void Create(SoraConfig config); 

    void Start();

    void Stop();

    //TODO 返回值
    void SendMessage<T>(T message);
}