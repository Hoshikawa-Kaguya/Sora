using System.Net.WebSockets;
using Sora.Enumeration;
using Sora.Interfaces;
using Websocket.Client;

namespace Sora.Entities.Socket;

/// <summary>
/// socket实例的简单封装，用于发送和控制的简单映射
/// </summary>
internal class ClientSocket : ISoraSocket
{
    private WebsocketClient _websocketClient;

    public object SocketInstance
    {
        get => _websocketClient;
        set => _websocketClient = value as WebsocketClient;
    }

    public SoraSocketType SocketType => SoraSocketType.Client;

    public ClientSocket(WebsocketClient connection)
    {
        _websocketClient = connection;
    }

    public void Send(string message)
    {
        _websocketClient.Send(message);
    }

    public void Close()
    {
        _websocketClient.Stop(WebSocketCloseStatus.Empty, "socket closed");
    }
}