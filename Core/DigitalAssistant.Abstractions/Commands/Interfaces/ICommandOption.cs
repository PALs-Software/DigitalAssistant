namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandOption
{
    string Name { get; }

    List<ICommandOptionValue> Values { get; }
}