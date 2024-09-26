using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Commands.Interpreter;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Connectors.Services;
using DigitalAssistant.Server.Modules.Setups.Enums;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class CommandProcessor(IServiceProvider serviceProvider,
    CommandHandler commandHandler,
    CommandParameterParser commandParameterParser,
    ConnectorService connectorService,
    ClientCommandService clientCommandService,
    CommandRegularExpressionInterpreter commandRegularExpressionInterpreter,
    CommandLlmInterpreter commandLlmInterpreter,
    IStringLocalizer<CommandProcessor> localizer,
    ILogger<CommandProcessor> logger)
{
    #region Injects
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly CommandHandler CommandHandler = commandHandler;
    protected readonly CommandParameterParser CommandParameterParser = commandParameterParser;
    protected readonly ConnectorService ConnectorService = connectorService;
    protected readonly ClientCommandService ClientCommandService = clientCommandService;
    protected readonly CommandLlmInterpreter CommandLlmInterpreter = commandLlmInterpreter;
    protected readonly CommandRegularExpressionInterpreter CommandRegularExpressionInterpreter = commandRegularExpressionInterpreter;
    protected readonly IStringLocalizer<CommandProcessor> Localizer = localizer;
    protected readonly ILogger<CommandProcessor> Logger = logger;
    #endregion

    public async Task<string?> ProcessUserCommandAsync(string userCommand, string language, IClient client, IServiceProvider serviceProvider)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            (ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters) matchedCommand = default;
            switch (Cache.SetupCache.Setup?.InterpreterMode)
            {
                case InterpreterMode.RegularExpression:
                    matchedCommand = await CommandRegularExpressionInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
                case InterpreterMode.LLM:
                    matchedCommand = await CommandLlmInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
                case InterpreterMode.Mixed:
                    matchedCommand = await CommandRegularExpressionInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    if (matchedCommand == default || matchedCommand.Command == null || matchedCommand.Parameters == null)
                        matchedCommand = await CommandLlmInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
            }
#if DEBUG
            stopwatch.Stop();
            Logger.LogInformation("Interpreting '{UserCommand}' took {ElapsedMilliseconds}ms", userCommand, stopwatch.ElapsedMilliseconds);
#endif

            if (matchedCommand == default || matchedCommand.Command == null || matchedCommand.Parameters == null)
                return Localizer["No Command found for \"{0}\"", userCommand];

            var response = await matchedCommand.Command.ExecuteAsync(matchedCommand.Parameters);

            if (!response.Success)
                return Localizer["No Command found for \"{0}\"", userCommand];

            var clientActionResponses = new List<Task<ClientActionResponse>>();
            if (response.Success && response.ClientActions.Count > 0)
                foreach (var clientAction in response.ClientActions)
                    clientActionResponses.Add(ClientCommandService.ExecuteClientActionAsync(language, clientAction.Client, clientAction.Action, serviceProvider));

            if (response.Success && response.DeviceActions.Count > 0)
                _ = Task.Run(async () =>
                {
                    foreach (var deviceAction in response.DeviceActions)
                        await ConnectorService.ExecuteDeviceActionAsync(deviceAction.Device, deviceAction.Action);
                });

            
            var responseText = response.Response ?? String.Empty;
            await Task.WhenAll(clientActionResponses);
            foreach (var clientActionResponse in clientActionResponses)
            {
                if (!String.IsNullOrEmpty(clientActionResponse.Result.Response))
                    responseText += clientActionResponse.Result.Response;
            }

            return responseText;
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    public async Task<string> ProcessUserCommandDebugAsync(string userCommand, string language, IClient client, IServiceProvider serviceProvider)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            var templates = await CommandHandler.GetLocalizedCommandTemplatesAsync(language);
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            (ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters) matchedCommand = default;
            switch (Cache.SetupCache.Setup?.InterpreterMode)
            {
                case InterpreterMode.RegularExpression:
                    matchedCommand = await CommandRegularExpressionInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
                case InterpreterMode.LLM:
                    matchedCommand = await CommandLlmInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
                case InterpreterMode.Mixed:
                    matchedCommand = await CommandRegularExpressionInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    if (matchedCommand == default || matchedCommand.Command == null || matchedCommand.Parameters == null)
                        matchedCommand = await CommandLlmInterpreter.InterpretUserCommandAsync(userCommand, language, client);
                    break;
            }

            stopwatch.Stop();
            Logger.LogInformation("Parsing and matching command '{UserCommand}' took {ElapsedMilliseconds}ms", userCommand, stopwatch.ElapsedMilliseconds);

            var response = $" - Command Matching Time: {stopwatch.ElapsedMilliseconds}ms" + Environment.NewLine;
            response += $" - Language: {language}" + Environment.NewLine;
            response += Environment.NewLine;

            if (matchedCommand == default || matchedCommand.Command == null || matchedCommand.Parameters == null)
                return response + "No Command found";

            var commandToExecute = matchedCommand.Command;
            var commandParameters = matchedCommand.Parameters;
            var matchedCommandTemplate = matchedCommand.Template;

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
                        var clientActionResult = await ClientCommandService.ExecuteClientActionAsync(language, clientAction.Client, clientAction.Action, serviceProvider);
                        response += $" - Success: {clientActionResult.Success}" + Environment.NewLine;
                        response += $" - Response: {clientActionResult.Response}" + Environment.NewLine;

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
