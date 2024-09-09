using DigitalAssistant.Base.ClientServerConnection;

namespace DigitalAssistant.Server.Modules.Clients.Models;

public record ClientTcpMessage(ClientConnection ClientConnection, TcpMessageType Type, Guid EventId, byte[] Data) : TcpMessage(Type, EventId, Data)
{
}
