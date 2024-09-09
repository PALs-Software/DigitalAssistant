using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.Audio.Windows.Provider;
using DigitalAssistant.Client.Modules.General;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Reflection;

namespace DigitalAssistant.Client.Modules.Audio.Windows;

public class WindowsAudioPlayer : IAudioPlayer
{
    #region Injects
    protected readonly ClientSettings Settings;
    protected readonly WindowsAudioDeviceService AudioDeviceService;
    #endregion

    #region Members

    protected Dictionary<AudioType, IWavePlayer?> OutputDevices = new() { { AudioType.Speech, null }, { AudioType.SoundEffect, null }, { AudioType.Stream, null } };
    protected Dictionary<AudioType, WaveFormat?> WaveFormats = new() { { AudioType.Speech, null }, { AudioType.SoundEffect, null }, { AudioType.Stream, null } };
    protected Dictionary<AudioType, MixingSampleProvider?> Mixer = new() { { AudioType.Speech, null }, { AudioType.SoundEffect, null }, { AudioType.Stream, null } };

    protected Dictionary<SoundEffect, CachedSound> SoundEffects = [];

    #region Stream
    protected Task? StreamTask;
    protected bool StreamIsPlaying;
    protected bool StopStream;
    protected string? CurrentlyPlayedStreamUrl = null;
    protected SemaphoreSlim StreamSemaphore = new(1, 1);
    protected WaveChannel32? VolumeStream;
    #endregion

    #endregion

    #region Init

    public WindowsAudioPlayer(ClientSettings settings, IAudioDeviceService audioDeviceService)
    {
        Settings = settings;
        AudioDeviceService = (WindowsAudioDeviceService)audioDeviceService;

        InitSoundEffects();
        InitOutputDevice(AudioType.Speech, Settings.VoiceAudioOutputSampleRate, 1);
        InitOutputDevice(AudioType.SoundEffect, 44100, 2);

        SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        SoftRestartService.OnSoftRestart += async (sender, args) => await OnSoftRestartAsync();
    }

    protected virtual async Task OnSoftRestartAsync()
    {
        await StopAllAsync();

        OutputDevices[AudioType.Speech]?.Dispose();
        OutputDevices[AudioType.SoundEffect]?.Dispose();

        InitOutputDevice(AudioType.Speech, Settings.VoiceAudioOutputSampleRate, 1);
        InitOutputDevice(AudioType.SoundEffect, 44100, 2);

        SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
    }

    protected void InitOutputDevice(AudioType audioType, int sampleRate, int channels)
    {
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        var outputDevice = new WasapiOut(AudioDeviceService.GetOutputDevice(Settings), AudioClientShareMode.Shared, useEventSync: true, 200);
        var mixer = new MixingSampleProvider(waveFormat)
        {
            ReadFully = true
        };
        outputDevice.Init(mixer);
        outputDevice.Play();

        OutputDevices[audioType] = outputDevice;
        Mixer[audioType] = mixer;
        WaveFormats[audioType] = waveFormat;
    }

    protected void InitSoundEffects()
    {
        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        ArgumentNullException.ThrowIfNull(executionDirectory);
        foreach (var soundEffect in Enum.GetValues<SoundEffect>())
        {
            var path = Path.Combine(executionDirectory, @$"Resources\{soundEffect}.wav");
            SoundEffects.Add(soundEffect, new CachedSound(path));
        }
    }
    #endregion

    #region Play

    public Task PlayAsync(byte[] audioData)
    {
        OutputDevices[AudioType.Speech]?.Play();
        Mixer[AudioType.Speech]?.AddMixerInput(new RawSourceWaveStream(audioData, 0, audioData.Length, WaveFormats[AudioType.Speech]));
        return Task.CompletedTask;
    }

    public Task PlayAsync(SoundEffect soundEffect)
    {
        OutputDevices[AudioType.SoundEffect]?.Play();
        Mixer[AudioType.SoundEffect]?.AddMixerInput(new CachedSoundSampleProvider(SoundEffects[soundEffect]));
        return Task.CompletedTask;
    }

    public async Task PlayAsync(string url)
    {
        await StreamSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (StreamIsPlaying)
                await StopAsync(AudioType.Stream, waitUntilStopped: true);

            StreamIsPlaying = true;
            StopStream = false;
        }
        finally
        {
            StreamSemaphore.Release();
        }

        StreamTask = Task.Factory.StartNew(async () =>
        {
            using var mediaFoundationReader = new MediaFoundationReader(url);
            var volumeStream = new WaveChannel32(mediaFoundationReader);
            using var waveOut = new WasapiOut(AudioDeviceService.GetOutputDevice(Settings), AudioClientShareMode.Shared, useEventSync: true, 200);
            VolumeStream = volumeStream;

            waveOut.Init(volumeStream);
            waveOut.Play();
            OutputDevices[AudioType.Stream] = waveOut;

            while ((waveOut.PlaybackState == PlaybackState.Playing || waveOut.PlaybackState == PlaybackState.Paused) && !StopStream)
                await Task.Delay(1000);

            StreamIsPlaying = false;
            StopStream = false;
            VolumeStream = null;
            CurrentlyPlayedStreamUrl = null;

        }, TaskCreationOptions.LongRunning);
    }
    #endregion

    #region Stop/Pause/Resume
    protected Task StopAllAsync()
    {
        return Task.WhenAll(StopAsync(AudioType.Speech), StopAsync(AudioType.SoundEffect), StopAsync(AudioType.Stream));
    }

    public async Task StopAsync(AudioType audioType, bool waitUntilStopped = false)
    {
        var outputDevice = OutputDevices[audioType];
        outputDevice?.Stop();

        if (audioType == AudioType.Stream)
        {
            StopStream = true;
            while (waitUntilStopped && StreamIsPlaying)
                await Task.Delay(100);
        }
    }

    public Task PauseAsync(AudioType audioType)
    {
        var outputDevice = OutputDevices[audioType];
        outputDevice?.Pause();
        return Task.CompletedTask;
    }

    public Task ResumeAsync(AudioType audioType)
    {
        var outputDevice = OutputDevices[audioType];
        outputDevice?.Play();
        return Task.CompletedTask;
    }
    #endregion

    #region Set/Is

    public bool IsPlaying(AudioType audioType)
    {
        return OutputDevices[audioType]?.PlaybackState == PlaybackState.Playing;
    }

    public bool IsPaused(AudioType audioType)
    {
        return OutputDevices[audioType]?.PlaybackState == PlaybackState.Paused;
    }

    public void SetVolume(AudioType audioType, float volume)
    {
        if (audioType == AudioType.Stream)
        {
            if (VolumeStream != null && !StopStream)
                VolumeStream.Volume = volume;
            return;
        }

        var outputDevice = OutputDevices[audioType];
        if (outputDevice == null)
            return;

        outputDevice.Volume = volume;
    }

    #endregion

    public void Dispose()
    {
        OutputDevices[AudioType.Speech]?.Stop();
        OutputDevices[AudioType.SoundEffect]?.Stop();
        StopStream = true;
        OutputDevices[AudioType.Stream]?.Stop();

        OutputDevices[AudioType.Speech]?.Dispose();
        OutputDevices[AudioType.SoundEffect]?.Dispose();
        OutputDevices[AudioType.Stream]?.Dispose();
    }
}
