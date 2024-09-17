using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.AudioPlayer;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Clients.BrowserClient.Commands;

public class BrowserSystemCommandHandler(ClientState clientState, WebAudioPlayer webAudioPlayer, IStringLocalizer<BrowserSystemCommandHandler> localizer)
{
    #region Injects
    protected readonly ClientState ClientState = clientState;
    protected readonly WebAudioPlayer WebAudioPlayer = webAudioPlayer;
    protected readonly IStringLocalizer<BrowserSystemCommandHandler> Localizer = localizer;
    #endregion

    public Task<ClientActionResponse> ProcessSystemActionAsync(SystemActionArgs args)
    {
        if (args.StopCurrentAction.GetValueOrDefault())
            return HandleStopCommandAsync();

        if (args.PauseCurrentAction.GetValueOrDefault())
            return HandlePauseCommandAsync();

        if (args.ContinueLastAction.GetValueOrDefault())
            return HandleContinueCommandAsync();

        if (args.IncreaseVolume.GetValueOrDefault() || args.DecreaseVolume.GetValueOrDefault() || args.SetVolume != null)
            return Task.FromResult(new ClientActionResponse(false, Localizer["CommandNotSupportedError"]));

        return Task.FromResult(new ClientActionResponse(false, null));
    }

    protected async Task<ClientActionResponse> HandleStopCommandAsync()
    {
        ClientState.StopLongRunningActionIfExists<MusicActionArgs>();
        await WebAudioPlayer.PauseAudioAsync().ConfigureAwait(false); ;
        return new ClientActionResponse(true, null);
    }

    protected async Task<ClientActionResponse> HandlePauseCommandAsync()
    {
        ClientState.StopLongRunningActionIfExists<MusicActionArgs>();
        await WebAudioPlayer.PauseAudioAsync().ConfigureAwait(false); ;
        return new ClientActionResponse(true, null);
    }

    protected async Task<ClientActionResponse> HandleContinueCommandAsync()
    {
        var clientState = ClientState;
        var args = clientState.GetLastLongRunningActionsIfExists<MusicActionArgs>();
        if (args == null || string.IsNullOrEmpty(args.MusicStreamUrl))
            return new ClientActionResponse(false, Localizer["NoTaskToContinueError"]);

        clientState.ReplaceLongRunningAction(args);
        await WebAudioPlayer.PlayAudioFromUrlAsync(args.MusicStreamUrl).ConfigureAwait(false); ;

        return new ClientActionResponse(true, null);
    }
}
