using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Server.Modules.Ai.Llm.Services;
using DigitalAssistant.Server.Modules.Commands.Parser;
using DigitalAssistant.Server.Modules.Commands.Services;

namespace DigitalAssistant.Server.Modules.Commands.Interpreter;

public class CommandLlmInterpreter : ICommandInterpreter
{
    #region Injects
    protected readonly CommandHandler CommandHandler;
    protected readonly LlmService LlmService;
    protected readonly CommandParameterParser CommandParameterParser;
    protected readonly CommandTemplateParser CommandTemplateParser;
    #endregion

    #region Members
    protected Dictionary<string, (ICommand Command, ICommandTemplate? Template)> Commands = [];
    protected string LlmFunctions;

    protected List<string> Clients = [];
    protected List<(string Name, List<string> AlternativeNames, DeviceType Type)> Devices = [];
    #endregion

    #region System Prompt
    protected string SystemPrompt = String.Empty;
    protected string SystemPromptTemplate = @"You are a digital assistant that can help users with various tasks.
Decide which function is to be executed based on the user request and return only this one function as the result.
;
You can execute the following functions:
%%COMMANDFUNCTIONTEMPLATEPLACEHOLDER%%
None() = Undefined command. No valid command found. Use if the user's input doesn't match any function.
;
%%EXTENDEDTEMPLATEPLACEHOLDER%%
;
Reminder:
Function calls must follow the format specified.
All required parameters must be provided.
If a group name is provided use the ByGroup function.
Only one function call should be returned per user request.
Include all parameter names in the response.
Return the function call as a single line.
If the user's input doesn't match any function, return None().";
    #endregion

    public CommandLlmInterpreter(CommandHandler commandHandler, LlmService llmService, CommandParameterParser commandParameterParser, CommandTemplateParser commandTemplateParser)
    {
        CommandHandler = commandHandler;
        LlmService = llmService;
        CommandParameterParser = commandParameterParser;
        CommandTemplateParser = commandTemplateParser;

        LlmFunctions = String.Empty;
        PrepareCommands();
    }

    public void PrepareCommands()
    {
        var commands = CommandHandler.GetCommands().OrderBy(entry => entry.Priority).ToList();
        foreach (var command in commands)
        {
            foreach (var functionTemplate in command.LlmFunctionTemplates)
            {
                if (String.IsNullOrEmpty(functionTemplate))
                    continue;

                var functionName = ICommand.GetLlmFunctionName(functionTemplate);
                if (functionName == null)
                    continue;

                ICommandTemplate? commandTemplate = null;
                var parameters = ICommand.GetLlmParameters(functionTemplate, trimOptionalCharacter: true);
                if (parameters.Count > 0)
                {
                    var parameterTemplate = String.Empty;
                    foreach (var parameter in parameters)
                        parameterTemplate += $"{{{parameter.Key}:{parameter.Value}}}";

                    commandTemplate = CommandTemplateParser.ParseTemplate(command, parameterTemplate, String.Empty);
                }

                if (Commands.ContainsKey(functionName))
                    throw new Exception($"The llm function name \"{functionName}\" is declaried twice, that is not allowed. Each function name can only be used once");

                Commands.Add(functionName, (command, commandTemplate));

                var functionTemplateText = functionTemplate.Replace("Boolean", "On or Off");
                LlmFunctions += $"{functionTemplateText} = {command.LlmFunctionDescription}" + Environment.NewLine;
            }
        }
        LlmFunctions = TrimEnd(LlmFunctions, Environment.NewLine);
    }

    public void SetTemplateNames(List<string> clients, List<(string Name, List<string> AlternativeNames, DeviceType Type)> devices, List<(string Name, List<string> AlternativeNames)> groups)
    {
        List<string?> extendedTemplates = [];
        extendedTemplates.Add(GetGroupDescription("You can control the following groups:", "Group", groups));
        extendedTemplates.Add(GetDeviceDescription("You can control the following lights:", "LightDevice", DeviceType.Light, devices));
        extendedTemplates.Add(GetDeviceDescription("You can control the following switches:", "SwitchDevice", DeviceType.Switch, devices));
        extendedTemplates = extendedTemplates.Where(entry => entry != null).ToList();

        SystemPrompt = SystemPromptTemplate
                        .Replace("%%COMMANDFUNCTIONTEMPLATEPLACEHOLDER%%", LlmFunctions)
                        .Replace("%%EXTENDEDTEMPLATEPLACEHOLDER%%", String.Join(Environment.NewLine + ";" + Environment.NewLine, extendedTemplates));
    }

    public async Task<(ICommand Command, ICommandTemplate? Template, ICommandParameters? Parameters)> InterpretUserCommandAsync(string userCommand, string language, IClient client)
    {
        var result = await LlmService.GenerateAnswerAsync(SystemPrompt, userCommand, ")", maxLength: 200);
        if (String.IsNullOrEmpty(result) || !result.Contains("("))
            return default;

        var functionName = ICommand.GetLlmFunctionName(result);
        if (String.IsNullOrEmpty(functionName))
            return default;

        if (!Commands.TryGetValue(functionName, out var command))
            return default;

        var commandParameterDictionary = new Dictionary<ICommandParameter, string>();
        if (command.Template != null)
        {
            var parameters = ICommand.GetLlmParameters(result);
            if (String.IsNullOrEmpty(functionName))
                return default;

            foreach (var parameter in parameters)
            {
                parameters[parameter.Key] = parameters[parameter.Key].Trim('\"', '\'');

                if (parameters[parameter.Key].Equals("on", StringComparison.OrdinalIgnoreCase))
                    parameters[parameter.Key] = "1";
                if (parameters[parameter.Key].Equals("off", StringComparison.OrdinalIgnoreCase))
                    parameters[parameter.Key] = "0";

                if (command.Template.Parameters.TryGetValue(parameter.Key, out var commandParameter))
                    commandParameterDictionary.Add(commandParameter, parameters[parameter.Key]);
            }
        }

        var parsingResult = await CommandParameterParser.ParseParametersAsync(commandParameterDictionary, language, client, InterpreterMode.LLM);
        if (!parsingResult.Success)
            return default;

        return (command.Command, command.Template, parsingResult.CommandParameters);
    }

    #region MISC

    protected string? GetGroupDescription(string intro, string parameterType, List<(string Name, List<string> AlternativeNames)> groups)
    {
        var template = intro + Environment.NewLine;
        foreach (var item in groups)
        {
            template += $"{item.Name}: {parameterType}" + Environment.NewLine;

            foreach (var alternativeName in item.AlternativeNames)
                template += $"{alternativeName}: {parameterType}" + Environment.NewLine;
        }
        return TrimEnd(template, Environment.NewLine);
    }

    protected string? GetDeviceDescription(string intro, string parameterType, DeviceType deviceType, List<(string Name, List<string> AlternativeNames, DeviceType Type)> devices)
    {
        var filteredDevices = devices.Where(entry => entry.Type == deviceType);
        if (!filteredDevices.Any())
            return null;

        var devicesTemplate = intro + Environment.NewLine;
        foreach (var item in filteredDevices)
        {
            devicesTemplate += $"{item.Name}: {parameterType}" + Environment.NewLine;

            foreach (var alternativeName in item.AlternativeNames)
                devicesTemplate += $"{alternativeName}: {parameterType}" + Environment.NewLine;
        }
        return TrimEnd(devicesTemplate, Environment.NewLine);
    }

    protected string TrimEnd(string source, string value)
    {
        if (!source.EndsWith(value))
            return source;

        return source.Remove(source.LastIndexOf(value));
    }
    #endregion
}
