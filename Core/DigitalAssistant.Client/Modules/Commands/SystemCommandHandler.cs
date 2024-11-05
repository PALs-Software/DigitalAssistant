using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;

namespace DigitalAssistant.Client.Modules.Commands;

public class SystemCommandHandler(IAudioPlayer audioPlayer, ClientState clientState, ClientSettings settings)
{
    #region Injects
    protected readonly IAudioPlayer AudioPlayer = audioPlayer;
    protected readonly ClientState ClientState = clientState;
    protected readonly ClientSettings Settings = settings;
    #endregion

    public async Task<ClientActionResponse> ProcessSystemActionAsync(SystemActionArgs args)
    {
        if (args.StopCurrentAction.GetValueOrDefault())
            await HandleStopCommandAsync();

        if (args.PauseCurrentAction.GetValueOrDefault())
            await HandlePauseCommandAsync();

        if (args.ContinueLastAction.GetValueOrDefault())
            await HandleContinueCommandAsync();

        if (args.IncreaseVolume.GetValueOrDefault())
            await HandleIncreaseVolumeCommandAsync();

        if (args.DecreaseVolume.GetValueOrDefault())
            await HandleDecreaseVolumeCommandAsync();

        if (args.SetVolume != null)
            await HandleSetVolumeCommandAsync(args.SetVolume.Value);

        return new ClientActionResponse(true, null);
    }

    protected async Task HandleStopCommandAsync()
    {
        bool resumeStream = false;
        bool speechWasPlaying = false;
        if (AudioPlayer.IsPlaying(AudioType.SoundEffect))
        {
            await AudioPlayer.StopAsync(AudioType.SoundEffect);
            resumeStream = true;
        }

        if (AudioPlayer.IsPlaying(AudioType.Speech))
        {
            await AudioPlayer.StopAsync(AudioType.Speech);
            resumeStream = true;
            speechWasPlaying = true;
        }

        if (resumeStream)
        {
            if (OperatingSystem.IsWindows())
                AudioPlayer.SetVolume(AudioType.Stream, 1f);
            else
                await AudioPlayer.ResumeAsync(AudioType.Stream);
        }

        if (speechWasPlaying)
            return;

        if (AudioPlayer.IsPlaying(AudioType.Stream))
        {
            ClientState.StopLongRunningActionIfExists<MusicActionArgs>();
            await AudioPlayer.StopAsync(AudioType.Stream);
        }
    }

    protected async Task HandlePauseCommandAsync()
    {
        if (!AudioPlayer.IsPlaying(AudioType.Stream))
            return;

        ClientState.StopLongRunningActionIfExists<MusicActionArgs>();
        await AudioPlayer.StopAsync(AudioType.Stream);
    }

    protected async Task HandleContinueCommandAsync()
    {
        if (AudioPlayer.IsPlaying(AudioType.Stream))
            return;

        var args = ClientState.GetLastLongRunningActionsIfExists<MusicActionArgs>();
        if (args == null || String.IsNullOrEmpty(args.MusicStreamUrl))
            return;

        ClientState.ReplaceLongRunningAction(args);
        await AudioPlayer.PlayAsync(args.MusicStreamUrl);
    }

    protected Task HandleIncreaseVolumeCommandAsync()
    {
        Settings.OutputAudioVolume = Math.Min(1, Settings.OutputAudioVolume + 0.1f);
        AudioPlayer.SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        return Task.CompletedTask;
    }

    protected Task HandleDecreaseVolumeCommandAsync()
    {
        Settings.OutputAudioVolume = Math.Max(0, Settings.OutputAudioVolume - 0.1f);
        AudioPlayer.SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        return Task.CompletedTask;
    }

    protected Task HandleSetVolumeCommandAsync(float volume)
    {
        Settings.OutputAudioVolume = Math.Max(0, Math.Min(1, volume));
        AudioPlayer.SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        return Task.CompletedTask;
    }
}
