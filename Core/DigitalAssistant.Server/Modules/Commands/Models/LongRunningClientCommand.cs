using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Enums;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class LongRunningClientCommand
{
    public Guid Id { get; set; }

    public LongRunningClientCommandType Type { get; set; }

    public Client? Client { get; set; }
}
