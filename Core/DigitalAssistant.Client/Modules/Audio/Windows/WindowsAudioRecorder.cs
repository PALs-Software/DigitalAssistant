using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.General;
using NAudio.Wave;

namespace DigitalAssistant.Client.Modules.Audio.Windows;

public class WindowsAudioRecorder : IAudioRecorder
{
    #region Events
    public event EventHandler<Memory<float>>? OnDataAvailable;
    #endregion

    #region Injects
    protected readonly ILogger<WindowsAudioRecorder> Logger;
    protected readonly ClientSettings Settings;
    protected readonly WindowsAudioDeviceService AudioDeviceService;
    #endregion

    #region Properties
    public int SampleRate { get; set; } = 16000;
    #endregion

    #region Members

    protected IWaveIn? WaveIn;
    protected bool IsRecording = false;
    protected TimeSpan AudioRecordingMinDataReceiveIntervall = TimeSpan.FromSeconds(10);
    protected DateTime LastAudioDataReceived = DateTime.Now;
    protected SemaphoreSlim AudioRecorderSemaphore { get; init; } = new(1, 1);

    protected int NoOfSamplesRead = 0;
    protected float[] FloatBuffer = new float[9600]; // Buffer Size of WaveIn
    protected Int16[] SamplesBuffer = new Int16[9600]; // Buffer Size of WaveIn
    #endregion

    #region Init

    public WindowsAudioRecorder(ILogger<WindowsAudioRecorder> logger, ClientSettings settings, IAudioDeviceService audioDeviceService)
    {
        Logger = logger;
        Settings = settings;
        AudioDeviceService = (WindowsAudioDeviceService)audioDeviceService;

        SoftRestartService.OnSoftRestart += async (sender, args) => await OnSoftRestartAsync();
    }

    protected virtual async Task OnSoftRestartAsync()
    {
        await StopAsync();
        await StartAsync();
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
        await AudioRecorderSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var inputDevice = AudioDeviceService.GetInputDevice(Settings);
            if (inputDevice == null)
                return false;

            var deviceNumber = AudioDeviceService.GetDeviceNumberFromWasabiInputDevice(inputDevice);
            if (deviceNumber == null)
                return false;

            WaveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber.Value,
                WaveFormat = new WaveFormat(rate: SampleRate, bits: 16, channels: 1),
                BufferMilliseconds = 100
            };

            WaveIn.DataAvailable += WaveIn_DataAvailable;
            WaveIn.StartRecording();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            AudioRecorderSemaphore.Release();
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        LastAudioDataReceived = DateTime.UtcNow;

        NoOfSamplesRead = e.Buffer.Length / 2;
        Buffer.BlockCopy(e.Buffer, 0, SamplesBuffer, 0, e.Buffer.Length);

        for (int i = 0; i < NoOfSamplesRead; i++)
            FloatBuffer[i] = SamplesBuffer[i];

        OnDataAvailable?.Invoke(this, FloatBuffer.AsMemory()[..NoOfSamplesRead]);
    }

    public async Task<bool> StopAsync()
    {
        await AudioRecorderSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (WaveIn == null)
                return true;

            WaveIn.StopRecording();
            WaveIn.DataAvailable -= WaveIn_DataAvailable;
            WaveIn = null;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            AudioRecorderSemaphore.Release();
        }
    }

    public void Dispose()
    {
        WaveIn?.Dispose();
    }
}