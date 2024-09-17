using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.AudioPlayer;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Clients.BrowserClient.Commands;

public class BrowserTimerCommandHandler(ClientState clientState, WebAudioPlayer webAudioPlayer, IStringLocalizer<BrowserTimerCommandHandler> localizer)
{
    #region Injects
    protected readonly ClientState ClientState = clientState;
    protected readonly WebAudioPlayer WebAudioPlayer = webAudioPlayer;
    protected readonly IStringLocalizer<BrowserTimerCommandHandler> Localizer = localizer;
    #endregion

    public Task<ClientActionResponse> ProcessTimerActionAsync(TimerActionArgs args)
    {
        if (args.SetTimer.GetValueOrDefault())
            return HandleSetTimerCommandAsync(args);

        if (args.GetTimer.GetValueOrDefault())
            return HandleGetTimerCommandAsync(args);

        if (args.DeleteTimer.GetValueOrDefault())
            return HandleDeleteTimerCommandAsync(args);

        return Task.FromResult(new ClientActionResponse(false, null));
    }

    protected Task<ClientActionResponse> HandleSetTimerCommandAsync(TimerActionArgs args)
    {
        if (args.Duration == null)
            return Task.FromResult(new ClientActionResponse(false, Localizer["DurationNotSetError"]));

        args.CancellationTokenSource = new CancellationTokenSource();
        args.TimerEnd = DateTime.Now.Add(args.Duration.Value);
        args.TimerTask = Task.Delay(args.Duration.Value, args.CancellationTokenSource.Token).ContinueWith(task =>
        {
            WebAudioPlayer.PlayAudioAsync(SoundEffect.TimerRingtone);
        }, args.CancellationTokenSource.Token);

        ClientState.CurrentLongRunningActions.Add(args);
        return Task.FromResult(new ClientActionResponse(true, null));
    }

    protected Task<ClientActionResponse> HandleGetTimerCommandAsync(TimerActionArgs args)
    {
        var timer = GetLastTimer(args);
        if (timer == null || timer.TimerEnd == null)
            return Task.FromResult(new ClientActionResponse(false, String.IsNullOrEmpty(args.Name) ? Localizer["NoTimerSetError"] : Localizer["NoNamedTimerSetError", args.Name]));

        var timeLeft = timer.TimerEnd.Value - DateTime.Now;
        var moreThanOneHour = timeLeft.TotalHours > 1;
        var moreThanOneMinute = timeLeft.TotalMinutes > 1;

        var timeLeftResponse = moreThanOneHour ? Localizer["HoursAndMinutes", timeLeft.Hours, timeLeft.Minutes] :
            moreThanOneMinute ? Localizer["MinutesAndSeconds", (int)timeLeft.TotalMinutes, timeLeft.Seconds] :
            Localizer["Seconds", (int)timeLeft.TotalSeconds];

        var response = String.IsNullOrEmpty(args.Name) ? Localizer["TimerTimeLeft", timeLeftResponse] : Localizer["NamedTimerTimeLeft", args.Name, timeLeftResponse];
        return Task.FromResult(new ClientActionResponse(false, response));
    }

    protected Task<ClientActionResponse> HandleDeleteTimerCommandAsync(TimerActionArgs args)
    {
        var timer = GetLastTimer(args);
        if (timer == null)
            return Task.FromResult(new ClientActionResponse(false, String.IsNullOrEmpty(args.Name) ? Localizer["NoTimerSetError"] : Localizer["NoNamedTimerSetError", args.Name]));

        timer.CancellationTokenSource?.Cancel();
        ClientState.CurrentLongRunningActions.Remove(timer);

        var response = String.IsNullOrEmpty(args.Name) ? Localizer["TimerDeleted"] : Localizer["NamedTimerDeleted", args.Name];
        return Task.FromResult(new ClientActionResponse(false, response));

    }

    protected TimerActionArgs? GetLastTimer(TimerActionArgs args)
    {
        var longRunningActions = ClientState.GetCurrentLongRunningActions<TimerActionArgs>();
        if (longRunningActions.Count == 0)
            return null;

        if (String.IsNullOrEmpty(args.Name))
            return longRunningActions.Last();
        else
            return longRunningActions.Where(entry => entry.Name == args.Name).LastOrDefault();
    }
}
