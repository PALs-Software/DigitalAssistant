using DigitalAssistant.Abstractions.Commands.Interfaces;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandOptionValue : ICommandOptionValue
{
    public string Name { get; set; } = null!;

    public List<string> LocalizedValues { get; set; } = null!;
}
