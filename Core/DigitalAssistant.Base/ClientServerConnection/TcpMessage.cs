using DigitalAssistant.Abstractions.Clients.Interfaces;
using System.Text;

namespace DigitalAssistant.Base.ClientServerConnection;

public record TcpMessage(TcpMessageType Type, Guid EventId, byte[] Data)
{
    public const int MessageHeaderByteLength = 35;
    protected const string MessageStartToken = "|<SOM>|";

    public byte[] GetMessageHeaderBytes()
    {
        var messageStartTokenBytes = Encoding.ASCII.GetBytes(MessageStartToken); // 7 bytes
        var typeBytes = BitConverter.GetBytes((int)Type); // 4 bytes
        var eventIdBytes = EventId.ToByteArray(); // 16 bytes
        var dataLength = BitConverter.GetBytes(Data.LongLength); // 8 bytes

        byte[] messageHeaderBytes = new byte[messageStartTokenBytes.Length + typeBytes.Length + eventIdBytes.Length + dataLength.Length];
        Buffer.BlockCopy(messageStartTokenBytes, 0, messageHeaderBytes, 0, messageStartTokenBytes.Length);
        Buffer.BlockCopy(typeBytes, 0, messageHeaderBytes, messageStartTokenBytes.Length, typeBytes.Length);
        Buffer.BlockCopy(eventIdBytes, 0, messageHeaderBytes, messageStartTokenBytes.Length + typeBytes.Length, eventIdBytes.Length);
        Buffer.BlockCopy(dataLength, 0, messageHeaderBytes, messageStartTokenBytes.Length + typeBytes.Length + eventIdBytes.Length, dataLength.Length);

        return messageHeaderBytes;
    }

    public static (TcpMessageType Type, Guid EventId, long MessageLength) GetMessageHeaderData(byte[] messageHeader)
    {
        var type = (TcpMessageType)BitConverter.ToInt32(messageHeader.AsSpan()[7..11]);
        var eventId = new Guid(messageHeader.AsSpan()[11..27]);
        var messageLength = BitConverter.ToInt64(messageHeader.AsSpan()[27..]);

        return (type, eventId, messageLength);
    }

    public static TcpMessage CreateActionMessage<TClientActionArgs>(TcpMessageActionType actionType, IClientActionArgs args) where TClientActionArgs: IClientActionArgs
    {
        var tcpMessageAction = new TcpMessageAction(actionType, args);
        return new TcpMessage(TcpMessageType.Action, Guid.NewGuid(), tcpMessageAction.ToByteArray<TClientActionArgs>());
    }
}
