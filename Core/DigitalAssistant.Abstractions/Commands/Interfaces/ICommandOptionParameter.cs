namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandOptionParameter : ICommandParameter
{
    ICommandOption Option { get; }
}
