using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Groups.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class DecreaseLightDeviceBrightnessCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50003;

    public override string[] LlmFunctionTemplates => [
        "DecreaseLightBrightness(Name: LightDevice, Decrease: Integer?)",
        "DecreaseLightBrightnessByGroup(GroupName: Group, Decrease: Integer?)"
    ];
    public override string LlmFunctionDescription => "Decreases the brightness of the specified light to make it darker.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        var lightDevices = new List<ILightDevice>();
        if (parameters.TryGetValue<IGroup>("GroupName", out var group))
            lightDevices.AddRange(group.Devices.OfType<ILightDevice>());

        if (parameters.TryGetValue<ILightDevice>("Name", out var lightDevice))
            lightDevices.Add(lightDevice);

        if (lightDevices.Count == 0)
            return Task.FromResult(CreateResponse(success: false));

        parameters.TryGetValue<int?>("Brightness", out var brightness);
        parameters.TryGetValue<int?>("Decrease", out var decrease);
        if (decrease != null)
            brightness = decrease;

        var lightActionArgs = new LightActionArgs() { BrightnessDelta = -brightness ?? -20 };
        var responseText = GetRandomResponses("Responses", GetNonNullNameOfObjects(group, lightDevice), $"{Math.Abs((decimal)lightActionArgs.BrightnessDelta)}%");

        return Task.FromResult(CreateResponse(success: true, responseText, CreateActionForAllDevices(lightDevices, lightActionArgs)));
    }
}
