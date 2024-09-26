using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.CoreCommands;

public class SetTimerCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 1100;

    public override string LlmFunctionTemplate => "SetTimer(Hours: Decimal, Minutes: Decimal, Seconds: Decimal, Name: Text?)";
    public override string LlmFunctionDescription => $"Sets a timer for the specified duration. Hours = {JsonLocalizer["Hours"]}, Minutes = {JsonLocalizer["Minutes"]}, Seconds = {JsonLocalizer["Seconds"]}.";

    public override Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (parameters.InterpreterMode == InterpreterMode.LLM)
            return ProcessLlmParameters(parameters);

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

        if (timerTimeSpan == TimeSpan.Zero)
            return Task.FromResult(CreateResponse(success: false));

        string response = GetResponse(timerName, timerTimeSpan);
        var args = new TimerActionArgs() { Name = timerName, SetTimer = true, Duration = timerTimeSpan };

        return Task.FromResult(CreateResponse(success: true, response, [(parameters.Client, args)]));
    }

    protected Task<ICommandResponse> ProcessLlmParameters(ICommandParameters parameters)
    {
        var timerTimeSpan = TimeSpan.Zero;
     
        if (parameters.TryGetValue<decimal>("Hours", out var hours))
            AddDurationToTimeSpan(ref timerTimeSpan, hours, "Hours");

        if (parameters.TryGetValue<decimal>("Minutes", out var minutes))
            AddDurationToTimeSpan(ref timerTimeSpan, minutes, "Minutes");

        if (parameters.TryGetValue<decimal>("Seconds", out var seconds))
            AddDurationToTimeSpan(ref timerTimeSpan, seconds, "Seconds");

        parameters.TryGetValue<string>("Name", out var timerName);

        if (timerTimeSpan == TimeSpan.Zero)
            return Task.FromResult(CreateResponse(success: false));

        string response = GetResponse(timerName, timerTimeSpan);
        var args = new TimerActionArgs() { Name = timerName, SetTimer = true, Duration = timerTimeSpan };

        return Task.FromResult(CreateResponse(success: true, response, [(parameters.Client, args)]));
    }

    protected string GetResponse(string? timerName, TimeSpan duration)
    {
        var arguments = new List<object>();
        var template = String.IsNullOrWhiteSpace(timerName) ? String.Empty : "Named";

        if (duration.Hours != 0)
            arguments.Add($"{duration.Hours} {JsonLocalizer["Hours"]}");
        if (duration.Minutes != 0)
            arguments.Add($"{duration.Minutes} {JsonLocalizer["Minutes"]}");
        if (duration.Seconds != 0)
            arguments.Add($"{duration.Seconds} {JsonLocalizer["Seconds"]}");

        switch (arguments.Count)
        {
            case 1:
                template += "SingleResponse";
                break;
            case 2:
                template += "DoubleResponse";
                break;
            case 3:
                template += "TripleResponse";
                break;
        }

        if (!String.IsNullOrWhiteSpace(timerName))
            arguments.Insert(0, timerName);

        return JsonLocalizer[template, arguments: arguments.ToArray()];
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
