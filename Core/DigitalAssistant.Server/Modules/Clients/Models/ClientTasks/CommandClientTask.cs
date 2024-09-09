using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Clients.Enums;

namespace DigitalAssistant.Server.Modules.Clients.Models.ClientTasks;

public class CommandClientTask(Client client, ClientTaskType type) : ClientTask(client, type)
{
    public BufferList<float> AudioData { get; set; } = [];
}