using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Localization;
using Microsoft.Extensions.Localization;
using RadioBrowser;
using RadioBrowser.Models;

namespace DigitalAssistant.MusicCommands;

public class RadioCommand(IStringLocalizer localizer, IJsonStringLocalizer jsonLocalizer) : Command(localizer, jsonLocalizer)
{
    public override CommandType Type => CommandType.Direct;
    public override int Priority => 70000;

    public override string[] LlmFunctionTemplates => ["PlayRadio(Name: Text)"];
    public override string LlmFunctionDescription => "Play Radio.";

    #region Members
    private bool ApplicationRunsInDockerContainer { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }
    #endregion

    public override async Task<ICommandResponse> ExecuteAsync(ICommandParameters parameters)
    {
        SetUICulture(parameters.Language);

        if (!parameters.TryGetValue<string>("Name", out var radioStationName))
            return CreateResponse(success: false);

        radioStationName = radioStationName.TrimEnd('.').Trim('"');
        var radioBrowser = new RadioBrowserClient(apiUrl: ApplicationRunsInDockerContainer ? "de1.api.radio-browser.info" : null);
        var radioStations = await radioBrowser.Search.AdvancedAsync(new AdvancedSearchOptions
        {
            Name = radioStationName,
            Codec = "MP3"
        });
        radioStations = radioStations.Where(entry => !String.IsNullOrEmpty(entry.UrlResolved?.AbsoluteUri)).ToList();
        if (radioStations.Count == 0)
            return CreateResponse(success: true, JsonLocalizer["NoRadioStationFound", radioStationName]);

        var args = new MusicActionArgs()
        {
            MusicStreamUrl = radioStations.First().UrlResolved.AbsoluteUri
        };
                
        return CreateResponse(success: true, JsonLocalizer["PlayRadioStationResponse", radioStationName], [(parameters.Client, args)]);
    }
}