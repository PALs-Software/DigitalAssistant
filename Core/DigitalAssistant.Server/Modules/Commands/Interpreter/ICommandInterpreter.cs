using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Interfaces;

namespace DigitalAssistant.Server.Modules.Commands.Interpreter;

public interface ICommandInterpreter
{
    Task<(ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters)> InterpretUserCommandAsync(string userCommand, string language, IClient client);
}
