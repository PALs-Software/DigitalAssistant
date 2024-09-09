using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Server.Modules.Connectors.Services;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class CommandProcessor(IServiceProvider serviceProvider,
    CommandHandler commandHandler,
    ConnectorService connectorService,
    ClientCommandService clientCommandService,
    IStringLocalizer<CommandProcessor> localizer,
    ILogger<CommandProcessor> logger)
{
    #region Injects
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly CommandHandler CommandHandler = commandHandler;
    protected readonly ConnectorService ConnectorService = connectorService;
    protected readonly ClientCommandService ClientCommandService = clientCommandService;
    protected readonly IStringLocalizer<CommandProcessor> Localizer = localizer;
    protected readonly ILogger<CommandProcessor> Logger = logger;
    #endregion

    public async Task<string> ProcessUserCommandAsync(string userCommand, string language, IClient client)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            var parameterParser = ServiceProvider.GetRequiredService<CommandParameterParser>();
            var templates = await CommandHandler.GetLocalizedCommandTemplatesAsync(language);

#if DEBUG
        var stopwatch = new Stopwatch();
        stopwatch.Start();
#endif

            ConcurrentBag<(ICommand Command, ICommandParameters? Parameters)> matchedTemplates = [];
            Parallel.ForEach(templates, async (templatesFromCommand, parallelLoopState) =>
            {
                foreach (var commandTemplate in templatesFromCommand)
                {
                    var match = commandTemplate.Regex.Match(userCommand);
                    if (!match.Success)
                        continue;

                    (bool success, ICommandParameters? parsedCommandParameters) = await parameterParser.ParseParametersFromMatchAsync(commandTemplate, match, language, client);
                    if (!success)
                        continue;

                    matchedTemplates.Add((commandTemplate.Command, parsedCommandParameters));
                    break;
                }
            });

#if DEBUG
        stopwatch.Stop();
        Logger.LogInformation("Parsing and matching command '{UserCommand}' took {ElapsedMilliseconds}ms", userCommand, stopwatch.ElapsedMilliseconds);
#endif

            var matchedTemplate = matchedTemplates.OrderBy(entry => entry.Command.Priority).LastOrDefault();
            if (matchedTemplate == default || matchedTemplate.Command == null || matchedTemplate.Parameters == null)
                return Localizer["No Command found for \"{0}\"", userCommand];

            var response = await matchedTemplate.Command.ExecuteAsync(matchedTemplate.Parameters);

            if (!response.Success || String.IsNullOrEmpty(response.Response))
                return Localizer["No Command found for \"{0}\"", userCommand];

            if (response.Success && response.ClientActions.Count > 0)
                _ = Task.Run(async () =>
                {
                    foreach (var clientAction in response.ClientActions)
                        await ClientCommandService.ExecuteClientActionAsync(clientAction.Client, clientAction.Action);
                });

            if (response.Success && response.DeviceActions.Count > 0)
                _ = Task.Run(async () =>
                {
                    foreach (var deviceAction in response.DeviceActions)
                        await ConnectorService.ExecuteDeviceActionAsync(deviceAction.Device, deviceAction.Action);
                });

            return response.Response;
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    public async Task<string> ProcessUserCommandDebugAsync(string userCommand, string language, IClient client)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            var parameterParser = ServiceProvider.GetRequiredService<CommandParameterParser>();
            var templates = await CommandHandler.GetLocalizedCommandTemplatesAsync(language);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            ConcurrentBag<(ICommandTemplate Template, ICommand Command, ICommandParameters? Parameters)> matchedTemplates = [];

            Parallel.ForEach(templates, async (templatesFromCommand, parallelLoopState) =>
                {
                    foreach (var commandTemplate in templatesFromCommand)
                    {
                        var match = commandTemplate.Regex.Match(userCommand);
                        if (!match.Success)
                            continue;

                        (bool success, ICommandParameters? parsedCommandParameters) = await parameterParser.ParseParametersFromMatchAsync(commandTemplate, match, language, client);
                        if (!success)
                            continue;

                        matchedTemplates.Add((commandTemplate, commandTemplate.Command, parsedCommandParameters));
                        break;
                    }
                });

            stopwatch.Stop();
            Logger.LogInformation("Parsing and matching command '{UserCommand}' took {ElapsedMilliseconds}ms", userCommand, stopwatch.ElapsedMilliseconds);

            var response = $" - Command Matching Time: {stopwatch.ElapsedMilliseconds}ms" + Environment.NewLine;
            response += $" - Language: {language}" + Environment.NewLine;
            response += Environment.NewLine;

            var matchedTemplate = matchedTemplates.OrderBy(entry => entry.Command.Priority).LastOrDefault();
            if (matchedTemplate == default || matchedTemplate.Command == null || matchedTemplate.Parameters == null)
                return response + "No Command found";

            var commandToExecute = matchedTemplate.Command;
            var commandParameters = matchedTemplate.Parameters;
            var matchedCommandTemplate = matchedTemplate.Template;

            response += $"Command found: {commandToExecute.GetName()}" + Environment.NewLine;
            response += $" - Template: {matchedCommandTemplate?.Template}" + Environment.NewLine;
            response += $" - Matched Regex: {matchedCommandTemplate?.Regex?.ToString()}" + Environment.NewLine;
            response += Environment.NewLine;

            response += $"Parameters: " + Environment.NewLine;
            foreach (var parameter in commandParameters.Parameters)
                response += $" - {parameter.Value.Parameter.Name} ({parameter.Value.Parameter.Type}): {parameter.Value.Value}" + Environment.NewLine;
            response += Environment.NewLine;

            var executionResponse = await commandToExecute.ExecuteAsync(commandParameters);
            response += $"Execution Result: " + Environment.NewLine;
            response += $" - Success: {executionResponse.Success}" + Environment.NewLine;
            response += $" - Response: {executionResponse.Response}" + Environment.NewLine;
            response += $" - Action Count: {executionResponse.DeviceActions.Count}" + Environment.NewLine;
            response += Environment.NewLine;

            if (executionResponse.Success && executionResponse.ClientActions.Count > 0)
            {
                response += $"Client Action Results: " + Environment.NewLine;
                foreach (var clientAction in executionResponse.ClientActions)
                {
                    response += $" - Client: {clientAction.Client.Name}" + Environment.NewLine;
                    response += $" - Action: {JsonSerializer.Serialize(clientAction.Action, clientAction.Action.GetType())}" + Environment.NewLine;
                    try
                    {
                        var clientActionResult = await ClientCommandService.ExecuteClientActionAsync(clientAction.Client, clientAction.Action);
                        response += $" - Success: {clientActionResult.Success}" + Environment.NewLine;
                        response += $" - Error Message: {clientActionResult.ErrorMessage}" + Environment.NewLine;

                    }
                    catch (Exception e)
                    {
                        response += $" - Success: {false}" + Environment.NewLine;
                        response += $" - Error Message: {e.Message}" + Environment.NewLine;
                    }
                    response += Environment.NewLine;
                }
            }

            if (executionResponse.Success && executionResponse.DeviceActions.Count > 0)
            {
                response += $"Device Action Results: " + Environment.NewLine;
                foreach (var deviceAction in executionResponse.DeviceActions)
                {
                    response += $" - Device: {deviceAction.Device.Name}" + Environment.NewLine;
                    response += $" - Action: {JsonSerializer.Serialize(deviceAction.Action, deviceAction.Action.GetType())}" + Environment.NewLine;
                    try
                    {
                        var deviceActionResult = await ConnectorService.ExecuteDeviceActionAsync(deviceAction.Device, deviceAction.Action);
                        response += $" - Success: {deviceActionResult.Success}" + Environment.NewLine;
                        response += $" - Error Message: {deviceActionResult.ErrorMessage}" + Environment.NewLine;

                    }
                    catch (Exception e)
                    {
                        response += $" - Success: {false}" + Environment.NewLine;
                        response += $" - Error Message: {e.Message}" + Environment.NewLine;
                    }
                    response += Environment.NewLine;
                }
            }

            return response;
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }
}
