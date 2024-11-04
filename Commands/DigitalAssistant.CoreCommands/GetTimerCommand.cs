using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.CoreCommands;

public class GetTimerCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 1101;

    public override string[] LlmFunctionTemplates => ["GetTimer(Name: Text?)"];
    public override string LlmFunctionDescription => "Get Timer.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        parameters.TryGetValue<string>("Name", out var timerName);
        
        var args = new TimerActionArgs() { Name = timerName, GetTimer = true };
        return Task.FromResult(CreateResponse(success: true, null, [(parameters.Client, args)]));
    }
}
