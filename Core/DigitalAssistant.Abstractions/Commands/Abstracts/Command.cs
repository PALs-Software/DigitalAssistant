using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Abstractions.Commands.Abstracts;

public abstract class Command : ICommand
{
    #region Injects
    protected readonly IStringLocalizer Localizer;
    protected readonly IJsonStringLocalizer JsonLocalizer;
    #endregion

    public Command(IStringLocalizer localizer, IJsonStringLocalizer jsonStringLocalizer)
    {
        Localizer = localizer;
        JsonLocalizer = jsonStringLocalizer;
        JsonLocalizer.LoadCompleteJsonIntoCacheByUse = true;
    }

    public abstract CommandType Type { get; }
    public virtual int Priority { get; } = 0;

    public abstract Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters);

    protected void SetUICulture(string language)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
    }

    public string GetName()
    {
        return JsonLocalizer["Name"];
    }

    public string GetDescription()
    {
        return JsonLocalizer["Description"];
    }

    public List<string> GetTemplates()
    {
        return JsonLocalizer.GetTranslationList("Templates");
    }

    public string GetOptionsJson()
    {
        return JsonLocalizer["Options"];
    }

    public List<string> GetResponses(string name = "Responses")
    {
        return JsonLocalizer.GetTranslationList(name);
    }

    public string GetRandomResponses(string name = "Responses", params object?[] args)
    {
        var responses = GetResponses(name);
        if (args.Length == 0)
            return responses[Random.Shared.Next(responses.Count)];
        else
            return String.Format(responses[Random.Shared.Next(responses.Count)], args);
    }

    protected ICommandResponse CreateResponse(bool success, string? response = null)
    {
        return new CommandResponse(success, response);
    }

    protected ICommandResponse CreateResponse(bool success, string? response, List<(IDevice Device, IDeviceActionArgs Action)> deviceActions)
    {
        return new CommandResponse(success, response, deviceActions);
    }

    protected ICommandResponse CreateResponse(bool success, string? response, List<(IClient Device, IClientActionArgs Action)> clientActions)
    {
        return new CommandResponse(success, response, clientActions);
    }
}
