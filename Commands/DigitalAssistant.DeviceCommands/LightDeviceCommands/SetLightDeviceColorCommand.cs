using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using System.Drawing;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class SetLightDeviceColorCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50007;

    public override string LlmFunctionTemplate => "SetLightColor(Name: LightDevice, Color: Color)";
    public override string LlmFunctionDescription => "Changes the color of the specified light.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ILightDevice>("Name", out var lightDevice) ||
            !parameters.TryGetValue<Color>("Color", out var color))
            return Task.FromResult(CreateResponse(success: false));

        var lightActionArgs = new LightActionArgs() { Color = $"#{color.R:X2}{color.G:X2}{color.B:X2}" };
        var responseText = GetRandomResponses("Responses", color.Name);

        return Task.FromResult(CreateResponse(success: true, responseText, [(lightDevice, lightActionArgs)]));
    }
}
