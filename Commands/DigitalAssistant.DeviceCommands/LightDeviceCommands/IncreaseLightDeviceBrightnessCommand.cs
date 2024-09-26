using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class IncreaseLightDeviceBrightnessCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50002;

    public override string LlmFunctionTemplate => "IncreaseLightBrightness(Name: LightDevice, Increase: Integer?)";
    public override string LlmFunctionDescription => "Increases the brightness of the specified light to make it brighter.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ILightDevice>("Name", out var lightDevice))
            return Task.FromResult(CreateResponse(success: false));

        parameters.TryGetValue<int?>("Brightness", out var brightness);
        parameters.TryGetValue<int?>("Increase", out var increase);
        if (increase != null)
            brightness = increase;

        var lightActionArgs = new LightActionArgs() { BrightnessDelta = brightness ?? 20 };
        var responseText = GetRandomResponses("Responses", lightDevice.Name, $"{Math.Abs((decimal)lightActionArgs.BrightnessDelta)}%");

        return Task.FromResult(CreateResponse(success: true, responseText, [(lightDevice, lightActionArgs)]));
    }   
}
