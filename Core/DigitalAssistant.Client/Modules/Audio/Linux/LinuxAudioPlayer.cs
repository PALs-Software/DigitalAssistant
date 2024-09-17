using System.Diagnostics;
using System.Reflection;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.General;

namespace DigitalAssistant.Client.Modules.Audio.Linux;

public class LinuxAudioPlayer : IAudioPlayer
{
    #region Events
    public event EventHandler<AudioType>? OnPlayFinished;
    #endregion

    #region Injects
    protected readonly ClientSettings Settings;
    protected readonly LinuxAudioDeviceService AudioDeviceService;
    #endregion

    #region Consts
    protected const string PauseProcessCommand = "kill -STOP {0}";
    protected const string ResumeProcessCommand = "kill -CONT {0}";
    #endregion

    #region Members

    protected Dictionary<AudioType, Process?> Processes = new() { { AudioType.Speech, null }, { AudioType.SoundEffect, null }, { AudioType.Stream, null } };
    protected Dictionary<AudioType, bool> AudioIsPlaying = new() { { AudioType.Speech, false }, { AudioType.SoundEffect, false }, { AudioType.Stream, false } };
    protected Dictionary<AudioType, bool> AudioIsPaused = new() { { AudioType.Speech, false }, { AudioType.SoundEffect, false }, { AudioType.Stream, false } };

    protected Dictionary<SoundEffect, byte[]> SoundEffects = [];

    #endregion

    #region Init

    public LinuxAudioPlayer(ClientSettings settings, IAudioDeviceService audioDeviceService)
    {
        Settings = settings;
        AudioDeviceService = (LinuxAudioDeviceService)audioDeviceService;

        InitSoundEffects();

        SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        SoftRestartService.OnSoftRestart += async (sender, args) => await OnSoftRestartAsync();
    }

    protected virtual Task OnSoftRestartAsync()
    {
        SetVolume(AudioType.Speech, Settings.OutputAudioVolume);
        return StopAllAsync();
    }

    protected void InitSoundEffects()
    {
        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        ArgumentNullException.ThrowIfNull(executionDirectory);
        foreach (var soundEffect in Enum.GetValues<SoundEffect>())
        {
            var path = Path.Combine(executionDirectory, "Resources", $"{soundEffect}.wav");
            SoundEffects.Add(soundEffect, File.ReadAllBytes(path));
        }
    }

    #endregion

    protected void OnInternalPlayFinished(AudioType audioType)
    {
        if (!AudioIsPlaying[audioType])
            return;

        AudioIsPlaying[audioType] = false;
        OnPlayFinished?.Invoke(this, audioType);
    }

    #region Play

    public async Task PlayAsync(AudioType audioType, byte[] audioData, string type, string format, int sampleRate, int channels)
    {
        await StopAsync(audioType).ConfigureAwait(false);

        var process = GetAPlayProcess(audioType, type, format, sampleRate, channels);
        Processes[audioType] = process;
        process.Start();
        await process.StandardInput.BaseStream.WriteAsync(audioData);
        await process.StandardInput.BaseStream.DisposeAsync();

        AudioIsPlaying[audioType] = true;
    }

    public Task PlayAsync(byte[] audioData)
    {
        return PlayAsync(AudioType.Speech, audioData, "raw", "FLOAT_LE", Settings.VoiceAudioOutputSampleRate, 1);
    }

    public Task PlayAsync(SoundEffect soundEffect)
    {
        return PlayAsync(AudioType.SoundEffect, SoundEffects[soundEffect], "wav", "S16_LE", 44100, 2);
    }

    public async Task PlayAsync(string url)
    {
        var audioType = AudioType.Stream;
        await StopAsync(audioType).ConfigureAwait(false);

        var outputDevice = AudioDeviceService.GetOutputDevice(Settings);
        var deviceSelection = outputDevice == null ? "" : $" -a \"{outputDevice}\"";

        var process = GetBashProcess($"LD_LIBRARY_PATH=/usr/local/lib;export LD_LIBRARY_PATH;mpg123{deviceSelection} -q {url}", addEvents: true, audioType: audioType);
        Processes[audioType] = process;
        process.Start();

        AudioIsPlaying[audioType] = true;
    }

    #endregion

    #region Stop/Pause/Resume
    protected Task StopAllAsync()
    {
        return Task.WhenAll(StopAsync(AudioType.Speech), StopAsync(AudioType.SoundEffect), StopAsync(AudioType.Stream));
    }

    public Task StopAsync(AudioType audioType, bool waitUntilStopped = false)
    {
        var process = Processes[audioType];
        if (process != null)
        {
            process.Kill();
            process.Dispose();
        }

        Processes[audioType] = null;
        AudioIsPlaying[audioType] = false;
        AudioIsPaused[audioType] = false;

        return Task.CompletedTask;
    }

    public async Task PauseAsync(AudioType audioType)
    {
        var process = Processes[audioType];
        if (!AudioIsPlaying[audioType] || AudioIsPaused[audioType] || process == null)
            return;

        var pauseProcess = GetBashProcess(String.Format(PauseProcessCommand, process.Id));
        pauseProcess.Start();
        await pauseProcess.WaitForExitAsync();

        AudioIsPaused[audioType] = true;
    }

    public async Task ResumeAsync(AudioType audioType)
    {
        var process = Processes[audioType];
        if (!AudioIsPlaying[audioType] || !AudioIsPaused[audioType] || process == null)
            return;

        var pauseProcess = GetBashProcess(String.Format(ResumeProcessCommand, process.Id));
        pauseProcess.Start();
        await pauseProcess.WaitForExitAsync();

        AudioIsPaused[audioType] = false;
    }

    #endregion

    #region Set/Is

    public bool IsPlaying(AudioType audioType)
    {
        return AudioIsPlaying[audioType];
    }

    public bool IsPaused(AudioType audioType)
    {
        return AudioIsPaused[audioType];
    }

    public void SetVolume(AudioType audioType, float volume)
    {
        byte percent = (byte)(volume * 100);
        var process = GetBashProcess($"amixer -M set 'Master' {percent}%");
        process.Start();
        process.WaitForExit();
    }

    #endregion

    #region Create Processes

    public Process GetProcess(string fileName, string arguments, bool addEvents, AudioType? audioType = null)
    {
        var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;

        if (addEvents)
        {
            ArgumentNullException.ThrowIfNull(audioType);

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => OnInternalPlayFinished(audioType.Value);
            process.ErrorDataReceived += (sender, e) => OnInternalPlayFinished(audioType.Value);
            process.Disposed += (sender, e) => OnInternalPlayFinished(audioType.Value);
        }

        return process;
    }

    public Process GetAPlayProcess(AudioType audioType, string type, string format, int sampleRate, int channels)
    {
        var outputDevice = AudioDeviceService.GetOutputDevice(Settings);
        var deviceSelection = outputDevice == null ? "" : $" -D \"{outputDevice}\"";
        return GetProcess("aplay", $"-q -t {type} -f {format} -r {sampleRate} -c {channels}{deviceSelection}", true, audioType);
    }

    public Process GetBashProcess(string command, bool addEvents = false, AudioType? audioType = null)
    {
        var escapedArgs = command.Replace("\"", "\\\"");
        return GetProcess("/bin/bash", $"-c \"{escapedArgs}\"", addEvents, audioType);
    }

    #endregion

    public void Dispose()
    {
        StopAllAsync().Wait();
    }
}