﻿using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class SetLightDeviceColorTemperatureCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50004;

    public override string LlmFunctionTemplate => "SetLightColorTemperature(Name: LightDevice, ColorTemperatureColor: ColorTemperatureColor)";
    public override string LlmFunctionDescription => "Adjusts the color temperature of the specified light.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ILightDevice>("Name", out var lightDevice))
            return Task.FromResult(CreateResponse(success: false));

        string? argText = null;
        var lightActionArgs = new LightActionArgs();
        if (parameters.TryGetValue<ColorTemperatureColor>("ColorTemperatureColor", out var colorTemperatureColor))
        {
            lightActionArgs.ColorTemperatureColor = colorTemperatureColor;
            argText = Localizer[colorTemperatureColor.ToString()];
        }
        else if(parameters.TryGetValue<int?>("ColorTemperature", out var colorTemperatur))
        {
            lightActionArgs.ColorTemperature = colorTemperatur;
            argText = colorTemperatur.ToString();
        }
        else
            return Task.FromResult(CreateResponse(success: false));

        var responseText = GetRandomResponses("Responses", lightDevice.Name, argText );
        return Task.FromResult(CreateResponse(success: true, responseText, [(lightDevice, lightActionArgs)]));
    }
}