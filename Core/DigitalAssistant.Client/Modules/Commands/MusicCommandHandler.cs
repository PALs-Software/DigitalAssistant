using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Client.Modules.Commands;

public class MusicCommandHandler(IStringLocalizer<MusicCommandHandler> localizer, IAudioPlayer audioPlayer, ClientState clientState)
{
    #region Injects
    protected readonly IStringLocalizer<MusicCommandHandler> Localizer = localizer;
    protected readonly IAudioPlayer AudioPlayer = audioPlayer;
    protected readonly ClientState ClientState = clientState;
    #endregion

    public async Task<ClientActionResponse> ProcessMusicActionAsync(MusicActionArgs args)
    {
        if (String.IsNullOrEmpty(args.MusicStreamUrl))
            return new ClientActionResponse(false, Localizer["NoMusicStreamUrlError"]);

        ClientState.ReplaceLongRunningAction(args);
        await AudioPlayer.PlayAsync(args.MusicStreamUrl).ConfigureAwait(false);

        return new ClientActionResponse(true, null);
    }
}
