using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Groups.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.GroupCommands;

public class SetGroupStateCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 70000;

    public override string[] LlmFunctionTemplates => [
        "SetGroupState(GroupName: Group, State: Boolean)"
    ];
    public override string LlmFunctionDescription => "Turns the specified group on or off. Example SetGroupState(Name: MyGroup, State: On).";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        var switchDevices = new List<ISwitchDevice>();
        var lightDevices = new List<ILightDevice>();
        if (!parameters.TryGetValue<IGroup>("GroupName", out var group))
            return Task.FromResult(CreateResponse(success: false));

        switchDevices.AddRange(group.Devices.OfType<ISwitchDevice>());
        lightDevices.AddRange(group.Devices.OfType<ILightDevice>());

        if (!parameters.TryGetValue<bool>("State", out var state))
            return Task.FromResult(CreateResponse(success: false));

        List<(IDevice Device, IDeviceActionArgs Action)> actions = [];
        actions.AddRange(CreateActionForAllDevices(switchDevices, new SwitchActionArgs() { On = state }));
        actions.AddRange(CreateActionForAllDevices(lightDevices, new LightActionArgs() { On = state }));

        var responseText = GetRandomResponses("Responses", group.Name, state ? JsonLocalizer["On"] : JsonLocalizer["Off"]);
        return Task.FromResult(CreateResponse(success: true, responseText, actions));
    }
}
