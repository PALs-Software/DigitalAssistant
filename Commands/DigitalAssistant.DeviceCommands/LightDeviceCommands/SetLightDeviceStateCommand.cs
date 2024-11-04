using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Groups.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.LightDeviceCommands;

public class SetLightDeviceStateCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 50000;

    public override string[] LlmFunctionTemplates => [
        "SetLightState(Name: LightDevice, State: Boolean)",
        "SetLightStateByGroup(GroupName: Group, State: Boolean)"
    ];
    public override string LlmFunctionDescription => "Turns the specified light on or off. Example SetLightState(Name: MyLight, State: On).";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        var lightDevices = new List<ILightDevice>();
        if (parameters.TryGetValue<IGroup>("GroupName", out var group))
            lightDevices.AddRange(group.Devices.OfType<ILightDevice>());

        if (parameters.TryGetValue<ILightDevice>("Name", out var lightDevice))
            lightDevices.Add(lightDevice);

        if (lightDevices.Count == 0 || !parameters.TryGetValue<bool>("State", out var state))
            return Task.FromResult(CreateResponse(success: false));

        var lightActionArgs = new LightActionArgs() { On = state };
        var responseText = GetRandomResponses("Responses", GetNonNullNameOfObjects(group, lightDevice), state ? JsonLocalizer["On"] : JsonLocalizer["Off"]);

        return Task.FromResult(CreateResponse(success: true, responseText, CreateActionForAllDevices(lightDevices, lightActionArgs)));
    }
}
