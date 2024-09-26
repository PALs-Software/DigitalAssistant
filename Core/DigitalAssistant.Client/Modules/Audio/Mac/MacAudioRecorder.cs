using System.Diagnostics;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.General;

namespace DigitalAssistant.Client.Modules.Audio.Mac;

public class MacAudioRecorder : IAudioRecorder
{
    #region Events
    public event EventHandler<Memory<float>>? OnDataAvailable;
    #endregion

    #region Injects
    protected readonly ILogger<MacAudioRecorder> Logger;
    protected readonly ClientSettings Settings;
    protected readonly MacAudioDeviceService AudioDeviceService;
    #endregion

    #region Properties
    public int SampleRate { get; set; } = 16000;
    #endregion

    #region Members
    protected Process? RecordProcess;
    protected Task? RecordTask;
    protected bool IsRecording = false;
    protected DateTime LastAudioDataReceived = DateTime.UtcNow;
    protected TimeSpan AudioRecordingMinDataReceiveIntervall = TimeSpan.FromSeconds(10);
    protected CancellationTokenSource RecordTaskTokenSource = new();

    #endregion


    #region Init

    public MacAudioRecorder(ILogger<MacAudioRecorder> logger, ClientSettings settings, IAudioDeviceService audioDeviceService)
    {
        Logger = logger;
        Settings = settings;
        AudioDeviceService = (MacAudioDeviceService)audioDeviceService;

        SoftRestartService.OnSoftRestart += async (sender, args) => await OnSoftRestartAsync();
    }

    protected virtual async Task OnSoftRestartAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await StartAsync().ConfigureAwait(false);
    }

    #endregion

    public async Task<bool> CheckIsStillRunningAsync()
    {
        if (IsRecording)
        {
            if (DateTime.UtcNow - LastAudioDataReceived < AudioRecordingMinDataReceiveIntervall)
                return true;
            else
            {
                Logger.LogWarning("Cannot receive audio data for more than {Intervall}s. The audio recorder will be stopped and then restarted.", AudioRecordingMinDataReceiveIntervall.Seconds);
                await StopAsync().ConfigureAwait(false);
            }
        }

        IsRecording = await StartAsync().ConfigureAwait(false);
        if (IsRecording)
            return true;

        Logger.LogError("Failed to start audio recorder");
        return false;
    }

    public async Task<bool> StartAsync()
    {
        await StopAsync().ConfigureAwait(false);

        RecordTaskTokenSource = new CancellationTokenSource();

        RecordProcess = new Process();
        RecordProcess.StartInfo.FileName = "ffmpeg ";
        RecordProcess.StartInfo.Arguments = $"-f avfoundation -i \":1\"";
        RecordProcess.StartInfo.UseShellExecute = false;
        RecordProcess.StartInfo.CreateNoWindow = true;
        RecordProcess.StartInfo.RedirectStandardOutput = true;
        RecordProcess.EnableRaisingEvents = true;
        RecordProcess.Exited += (sender, e) => OnInternalRecordFinished();
        RecordProcess.ErrorDataReceived += (sender, e) => OnInternalRecordFinished();
        RecordProcess.Disposed += (sender, e) => OnInternalRecordFinished();
        RecordProcess.Start();

        RecordTask = Task.Factory.StartNew(ReadRecordProcessData, TaskCreationOptions.LongRunning);

        IsRecording = true;
        return true;
    }

    protected async Task ReadRecordProcessData()
    {
        int bytesRead = 0;
        int noOfSamplesRead = 0;
        var buffer = new byte[(int)(0.5f * SampleRate)]; // 250ms
        var floatBuffer = new float[buffer.Length / 2];
        var samples = new Int16[buffer.Length / 2];
        do
        {
            if (RecordProcess == null)
                break;

            bytesRead = await RecordProcess.StandardOutput.BaseStream.ReadAsync(buffer, RecordTaskTokenSource.Token).ConfigureAwait(false);
            if (bytesRead >= 2)
            {
                LastAudioDataReceived = DateTime.Now;
                noOfSamplesRead = bytesRead / 2;
                Buffer.BlockCopy(buffer, 0, samples, 0, bytesRead);

                for (int i = 0; i < noOfSamplesRead; i++)
                    floatBuffer[i] = samples[i];

                OnDataAvailable?.Invoke(this, floatBuffer.AsMemory()[..noOfSamplesRead]);
            }

        } while (bytesRead != 0 && IsRecording && !RecordTaskTokenSource.IsCancellationRequested);
    }

    protected void OnInternalRecordFinished()
    {
        IsRecording = false;
        RecordTaskTokenSource.Cancel();
    }

    public async Task<bool> StopAsync()
    {
        RecordProcess?.Kill();
        RecordProcess?.Dispose();

        RecordProcess = null;
        IsRecording = false;
        RecordTaskTokenSource.Cancel();
        if (RecordTask != null)
            try { await RecordTask; } catch (Exception) { };
        RecordTask = null;

        return true;
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }
}