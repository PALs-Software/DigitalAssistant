using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Commands.SystemCommands;

public class StopCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => int.MaxValue;

    public override string[] LlmFunctionTemplates => ["Stop()"];
    public override string LlmFunctionDescription => "Stops the current action.";


    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        var systemActionArgs = new SystemActionArgs() { StopCurrentAction = true };

        return Task.FromResult(CreateResponse(success: true, null, [(parameters.Client, systemActionArgs)]));
    }
}