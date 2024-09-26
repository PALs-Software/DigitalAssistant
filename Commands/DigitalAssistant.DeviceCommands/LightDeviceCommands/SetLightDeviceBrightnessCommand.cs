using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class SetLightDeviceBrightnessCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50001;

    public override string LlmFunctionTemplate => "SetLightBrightness(Name: LightDevice, Brightness: Integer)";
    public override string LlmFunctionDescription => "Sets the brightness level of the specified light.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ILightDevice>("Name", out var lightDevice) ||
            !parameters.TryGetValue<int?>("Brightness", out var brightness))
            return Task.FromResult(CreateResponse(success: false));

        var lightActionArgs = new LightActionArgs() { Brightness = brightness };
        var responseText = GetRandomResponses("Responses", lightDevice.Name, $"{brightness}%");

        return Task.FromResult(CreateResponse(success: true, responseText, [(lightDevice, lightActionArgs)]));
    }   
}
