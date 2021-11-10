using Fleck;
using Sora.Enumeration;
using Sora.Interfaces;

namespace Sora.Entities.Socket;

/// <summary>
/// socket实例的简单封装，用于发送和控制的简单映射
/// </summary>
internal class ServerSocket : ISoraSocket
{
    private IWebSocketConnection _socketConnection;

    public object SocketInstance
    {
        get => _socketConnection;
        set => _socketConnection = value as IWebSocketConnection;
    }

    public SoraSocketType SocketType => SoraSocketType.Server;

    public ServerSocket(IWebSocketConnection connection)
    {
        _socketConnection = connection;
    }

    public void Send(string message)
    {
        _socketConnection.Send(message);
    }

    public void Close()
    {
        _socketConnection.Close();
    }
}