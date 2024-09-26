using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Commands.Services;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.Commands.Interpreter;

public class CommandRegularExpressionInterpreter(CommandHandler commandHandler, CommandParameterParser commandParameterParser) : ICommandInterpreter
{
    #region Injects
    protected readonly CommandHandler CommandHandler = commandHandler;
    protected readonly CommandParameterParser CommandParameterParser = commandParameterParser;
    #endregion

    public async Task<(ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters)> InterpretUserCommandAsync(string userCommand, string language, IClient client)
    {
        var templates = await CommandHandler.GetLocalizedCommandTemplatesAsync(language);

        ConcurrentBag<(ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters)> matchedTemplates = [];
        Parallel.ForEach(templates, async (templatesFromCommand, parallelLoopState) =>
        {
            foreach (var commandTemplate in templatesFromCommand)
            {
                var match = commandTemplate.Regex.Match(userCommand);
                if (!match.Success)
                    continue;

                (bool success, ICommandParameters? parsedCommandParameters) = await CommandParameterParser.ParseParametersFromMatchAsync(commandTemplate, match, language, client, InterpreterMode.RegularExpression);
                if (!success)
                    continue;

                matchedTemplates.Add((commandTemplate.Command, commandTemplate, parsedCommandParameters));
                break;
            }
        });

        return matchedTemplates.OrderBy(entry => entry.Command.Priority).FirstOrDefault();
    }
}
