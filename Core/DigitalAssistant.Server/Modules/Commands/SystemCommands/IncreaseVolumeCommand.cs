﻿using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Commands.SystemCommands;

public class IncreaseVolumeCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 2000000001;

    public override string[] LlmFunctionTemplates => ["IncreaseVolume()"];
    public override string LlmFunctionDescription => "Increases the volume of the device to make it louder.";


    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        var systemActionArgs = new SystemActionArgs() { IncreaseVolume = true };

        return Task.FromResult(CreateResponse(success: true, null, [(parameters.Client, systemActionArgs)]));
    }
}