using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace DigitalAssistant.Server.Modules.Commands.Models;

public class CommandParameters(IClient client, string language, InterpreterMode interpreterMode, IReadOnlyDictionary<string, (ICommandParameter Parameter, object? Value)> parameters) : ICommandParameters
{
    public IClient Client { get; init; } = client;
    public string Language { get; init; } = language;
    public InterpreterMode InterpreterMode { get; init; } = interpreterMode;
    public IReadOnlyDictionary<string, (ICommandParameter Parameter, object? Value)> Parameters { get; init; } = parameters;

    public bool TryGetValue<TValue>(string parameterName, [MaybeNullWhen(false)] out TValue value)
    {
        var success = Parameters.TryGetValue(parameterName, out var parameterTuple);

        if (success && parameterTuple.Value is TValue castedValue)
        {
            value = castedValue;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
}
