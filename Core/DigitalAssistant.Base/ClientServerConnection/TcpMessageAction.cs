using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Base.General;
using System.Text;
using System.Text.Json;

namespace DigitalAssistant.Base.ClientServerConnection;

public record TcpMessageAction(TcpMessageActionType Type, IClientActionArgs Args, string Language)
{
    public byte[] ToByteArray<TClientActionArgs>() where TClientActionArgs : IClientActionArgs
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonTypeMappingConverter<IClientActionArgs, TClientActionArgs>());
        var jsonData = JsonSerializer.Serialize(this, options);
        var binaryData = Encoding.UTF8.GetBytes(jsonData);

        return binaryData;
    }
}
