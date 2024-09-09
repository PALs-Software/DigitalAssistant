using DigitalAssistant.Abstractions.Commands.Enums;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandParameter
{
    string Name { get; }

    CommandParameterType Type { get; }

    bool IsOptional { get; }

    List<ICommandParameter> AlternativeParameters { get; }

    ICommandOptionParameter AsOptionParameter()
    {
        return (ICommandOptionParameter)this;
    }
}
