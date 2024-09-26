using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Commands.SystemCommands;

public class SetVolumeCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 2000000000;

    public override string LlmFunctionTemplate => "SetVolume(Volume: Integer))";
    public override string LlmFunctionDescription => "Set Volume to a specifc sound level.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<int>("Volume", out var volume))
            return Task.FromResult(CreateResponse(success: false));

        var systemActionArgs = new SystemActionArgs() { SetVolume = volume * 0.01f };

        return Task.FromResult(CreateResponse(success: true, null, [(parameters.Client, systemActionArgs)]));
    }
}