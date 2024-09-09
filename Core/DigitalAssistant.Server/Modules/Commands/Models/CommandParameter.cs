using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandParameter(string name, CommandParameterType type, bool isOptional) : ICommandParameter
{
    public string Name { get; init; } = name;

    public CommandParameterType Type { get; init; } = type;

    public bool IsOptional { get; init; } = isOptional;

    public List<ICommandParameter> AlternativeParameters { get; init; } = [];

    public ICommandOptionParameter AsOptionParameter()
    {
        return (ICommandOptionParameter)this;
    }
}
