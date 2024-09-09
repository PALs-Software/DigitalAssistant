using DigitalAssistant.Abstractions.Commands.Interfaces;
using System.Text.RegularExpressions;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandTemplate(ICommand command, string language, string template, Regex regex, IReadOnlyDictionary<string, ICommandParameter> parameters) : ICommandTemplate
{
    public ICommand Command { get; init; } = command;
    public string Language { get; init; } = language;
    public string Template { get; init; } = template;
    public Regex Regex { get; init; } = regex;
    public IReadOnlyDictionary<string, ICommandParameter> Parameters { get; init; } = parameters;
}
