using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.AudioPlayer;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Clients.BrowserClient.Commands;

public class BrowserMusicCommandHandler(ClientState clientState, WebAudioPlayer webAudioPlayer, IStringLocalizer<BrowserMusicCommandHandler> localizer)
{
    #region Injects
    protected readonly ClientState ClientState = clientState;
    protected readonly WebAudioPlayer WebAudioPlayer = webAudioPlayer;
    protected readonly IStringLocalizer<BrowserMusicCommandHandler> Localizer = localizer;
    #endregion

    public async Task<ClientActionResponse> ProcessMusicActionAsync(MusicActionArgs args)
    {
        if (string.IsNullOrEmpty(args.MusicStreamUrl))
            return new ClientActionResponse(false, Localizer["NoMusicStreamUrlError"]);

        ClientState.ReplaceLongRunningAction(args);
        await WebAudioPlayer.PlayAudioFromUrlAsync(args.MusicStreamUrl).ConfigureAwait(false);

        return new ClientActionResponse(true, null);
    }
}
