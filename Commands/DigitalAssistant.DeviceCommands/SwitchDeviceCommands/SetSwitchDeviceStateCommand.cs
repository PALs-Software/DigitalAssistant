using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.DeviceCommands.SwitchDeviceCommands;

public class SetSwitchDeviceStateCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 60000;

    public override string[] LlmFunctionTemplates => [
        "SetSwitchState(Name: LightDevice, State: Boolean)"
    ];
    public override string LlmFunctionDescription => "Turns the specified switch on or off. Example SetSwitchState(Name: MySwitch, State: On).";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<ISwitchDevice>("Name", out var switchDevice))
            return Task.FromResult(CreateResponse(success: false));

        if (!parameters.TryGetValue<bool>("State", out var state))
            return Task.FromResult(CreateResponse(success: false));

        var actionArgs = new SwitchActionArgs() { On = state };
        var responseText = GetRandomResponses("Responses", switchDevice.Name, state ? JsonLocalizer["On"] : JsonLocalizer["Off"]);

        return Task.FromResult(CreateResponse(success: true, responseText, [(switchDevice, actionArgs)]));
    }
}
