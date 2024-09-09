using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandOptionParameter(string name, CommandParameterType type, bool isOptional, ICommandOption option) : CommandParameter(name, type, isOptional), ICommandOptionParameter
{
    public ICommandOption Option { get; init; } = option;
}
