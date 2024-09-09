using DigitalAssistant.Server.Modules.Clients.Enums;

namespace DigitalAssistant.Server.Modules.Clients.Models.ClientTasks;

public class ClientTask(Client client, ClientTaskType type)
{
    public Client Client { get; init; } = client;
    public ClientTaskType Type { get; init; } = type;
}
