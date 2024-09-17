using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.CoreCommands;

public class SetTimerCommands(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<decimal>("Duration", out var duration))
            return Task.FromResult(CreateResponse(success: false));

        if (!parameters.TryGetValue<string>("DurationType", out var durationType))
            return Task.FromResult(CreateResponse(success: false));

        parameters.TryGetValue<string>("Name", out var timerName);

        var timerTimeSpan = TimeSpan.Zero;
        AddDurationToTimeSpan(ref timerTimeSpan, duration, durationType);

        var mixedValues = parameters.TryGetValue<decimal>("Duration2", out var duration2) & parameters.TryGetValue<string>("DurationType2", out var durationType2);
        if (mixedValues)
            AddDurationToTimeSpan(ref timerTimeSpan, duration2, durationType2!);

        string? response = null;
        var args = new TimerActionArgs() { Name = timerName, SetTimer = true, Duration = timerTimeSpan };

        if (String.IsNullOrEmpty(timerName))
        {
            if (mixedValues)
                response = JsonLocalizer["DoubleResponse", $"{duration} {JsonLocalizer[durationType]}", $"{duration2} {JsonLocalizer[(durationType2!)]}"];
            else
                response = JsonLocalizer["SingleResponse", $"{duration} {JsonLocalizer[durationType]}"];
        }
        else
        {
            if (mixedValues)
                response = JsonLocalizer["NamedDoubleResponse", timerName, $"{duration} {JsonLocalizer[durationType]}", $"{duration2} {JsonLocalizer[(durationType2!)]}"];
            else
                response = JsonLocalizer["NamedSingleResponse", timerName, $"{duration} {JsonLocalizer[durationType]}"];
        }

        return Task.FromResult(CreateResponse(success: true, response, [(parameters.Client, args)]));
    }

    protected void AddDurationToTimeSpan(ref TimeSpan timeSpan, decimal duration, string durationType)
    {
        switch (durationType)
        {
            case "Seconds":
                timeSpan = timeSpan.Add(TimeSpan.FromSeconds((double)duration));
                break;
            case "Minutes":
                timeSpan = timeSpan.Add(TimeSpan.FromMinutes((double)duration));
                break;
            case "Hours":
                timeSpan = timeSpan.Add(TimeSpan.FromHours((double)duration));
                break;
        }
    }
}
