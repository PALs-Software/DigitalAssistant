using DigitalAssistant.Abstractions.Commands.Enums;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommand
{
    CommandType Type { get; }
    int Priority { get; }

    Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters);

    string GetName();
    string GetDescription();
    List<string> GetTemplates();
    string GetOptionsJson();
}
