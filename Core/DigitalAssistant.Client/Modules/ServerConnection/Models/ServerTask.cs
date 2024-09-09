
using DigitalAssistant.Client.Modules.ServerConnection.Enums;

namespace DigitalAssistant.Client.Modules.ServerConnection.Models;

public class ServerTask(ServerTaskType type)
{
    public ServerTaskType Type { get; init; } = type;
}
