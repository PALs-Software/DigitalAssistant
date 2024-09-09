namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandOptionValue
{
    string Name { get; }

    List<string> LocalizedValues { get; }
}
