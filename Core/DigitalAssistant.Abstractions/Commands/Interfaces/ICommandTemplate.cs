using System.Text.RegularExpressions;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandTemplate
{
    ICommand Command { get; }
    string Language { get; }
    string Template { get; }
    Regex Regex { get; }
    IReadOnlyDictionary<string, ICommandParameter> Parameters { get; }
}
