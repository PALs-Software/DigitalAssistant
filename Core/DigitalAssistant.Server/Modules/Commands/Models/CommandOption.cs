using DigitalAssistant.Abstractions.Commands.Interfaces;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandOption : ICommandOption
{
    public string Name { get; set; } = null!;

    public List<ICommandOptionValue> Values { get; set; } = [];
}