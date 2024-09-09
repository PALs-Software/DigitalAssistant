using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace DigitalAssistant.Server.Tests.Commands;

public class DummyCommand(IStringLocalizer localizer,  IJsonStringLocalizer jsonStringLocalizer) : Command(localizer, jsonStringLocalizer)
{
    public override CommandType Type => CommandType.Direct;

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        return Task.FromResult((ICommandResponse)new CommandResponse(success: true, "Dummy Command Result"));
    }
}
