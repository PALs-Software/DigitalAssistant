﻿using DigitalAssistant.Abstractions.Clients.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandParameters
{
    IClient Client { get; }
    string Language { get; }
    IReadOnlyDictionary<string, (ICommandParameter Parameter, object? Value)> Parameters { get; }

    bool TryGetValue<TValue>(string parameterName, [MaybeNullWhen(false)] out TValue value);
}