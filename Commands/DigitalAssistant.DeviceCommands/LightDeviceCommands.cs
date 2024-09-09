using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using System.Drawing;

namespace DigitalAssistant.DeviceCommands;

public class LightDeviceCommands(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;


    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ILightDevice>("Light", out var lightDevice))
            return Task.FromResult(CreateResponse(success: false));

        string? responseText = null;
        var lightActionArgs = new LightActionArgs();

        var success = IsOnOffCommand(parameters, lightDevice, ref lightActionArgs, ref responseText) ||
                      IsColorTemperatureCommand(parameters, lightDevice, ref lightActionArgs, ref responseText) ||
                      IsBrightnessCommand(parameters, lightDevice, ref lightActionArgs, ref responseText) ||
                      IsColorCommand(parameters, lightDevice, ref lightActionArgs, ref responseText);

        if (success)
            return Task.FromResult(CreateResponse(success: true, responseText, [(lightDevice, lightActionArgs)]));
        else
            return Task.FromResult(CreateResponse(success: false));
    }

    protected bool IsOnOffCommand(ICommandParameters parameters, ILightDevice lightDevice, ref LightActionArgs args, ref string? responseText)
    {
        if (!parameters.TryGetValue<bool>("State", out var state))
            return false;

        args.On = state;
        responseText = GetRandomResponses("OnOffChangedResponse", lightDevice.Name, state ? JsonLocalizer["On"] : JsonLocalizer["Off"]);
        return true;
    }

    protected bool IsColorTemperatureCommand(ICommandParameters parameters, ILightDevice lightDevice, ref LightActionArgs args, ref string? responseText)
    {
        if (parameters.TryGetValue<ColorTemperatureColor>("ColorTemperatureColor", out var colorTemperatureColor))
        {
            args.ColorTemperatureColor = colorTemperatureColor;
            responseText = GetRandomResponses("ColorTemperatureColorChangedResponse", lightDevice.Name, Localizer[colorTemperatureColor.ToString()]);
            return true;
        }         

        if (!parameters.TryGetValue<string>("IsColorTemperature", out var isColorTemperatur) || isColorTemperatur != "IsColorTemperature")
            return false;

        parameters.TryGetValue<int?>("ColorTemperature", out var colorTemperatur);
        if (parameters.TryGetValue<string>("DeltaDirection", out var deltaDirection) && !String.IsNullOrEmpty(deltaDirection))
        {
            if (deltaDirection == "Increase")
                args.ColorTemperatureDelta = colorTemperatur ?? 20;
            else
                args.ColorTemperatureDelta = -colorTemperatur ?? -20;

            responseText = GetRandomResponses("ValueDeltaChangedResponse", JsonLocalizer["ColorTemperature"], lightDevice.Name, args.ColorTemperatureDelta >= 0 ? JsonLocalizer["Increased"] : JsonLocalizer["Decreased"], Math.Abs((decimal)args.ColorTemperatureDelta));
            return true;
        }
        else if (colorTemperatur != null)
        {
            args.ColorTemperature = colorTemperatur;
            responseText = GetRandomResponses("ValueChangedResponse", JsonLocalizer["ColorTemperature"], lightDevice.Name, args.ColorTemperature);
            return true;
        }
        else
            return false;
    }

    protected bool IsBrightnessCommand(ICommandParameters parameters, ILightDevice lightDevice, ref LightActionArgs args, ref string? responseText)
    {
        parameters.TryGetValue<int?>("Brightness", out var brightness);
        if (parameters.TryGetValue<string>("DeltaDirection", out var deltaDirection) && !String.IsNullOrEmpty(deltaDirection))
        {
            if (deltaDirection == "Increase")
                args.BrightnessDelta = brightness ?? 20;
            else
                args.BrightnessDelta = -brightness ?? -20;

            responseText = GetRandomResponses("ValueDeltaChangedResponse", JsonLocalizer["Brightness"], lightDevice.Name, args.BrightnessDelta >= 0 ? JsonLocalizer["Increased"] : JsonLocalizer["Decreased"], $"{Math.Abs((decimal)args.BrightnessDelta)}%");
            return true;
        }
        else if (brightness != null)
        {
            args.Brightness = brightness;
            responseText = GetRandomResponses("ValueChangedResponse", JsonLocalizer["Brightness"], lightDevice.Name, $"{args.Brightness}%");
            return true;
        }
        else
            return false;
    }

    protected bool IsColorCommand(ICommandParameters parameters, ILightDevice lightDevice, ref LightActionArgs args, ref string? responseText)
    {
        if (!parameters.TryGetValue<Color>("Color", out var color))
            return false;

        args.Color = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        responseText = GetRandomResponses("ColorChangedResponse", lightDevice.Name, color.Name);
        return true;
    }
}
