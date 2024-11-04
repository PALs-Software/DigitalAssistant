using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.ServerConnection.Services;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.Reflection;

namespace DigitalAssistant.Client.Modules.SpeechRecognition.Services;

public class WakeWordListener : TimerBackgroundService
{
    protected override TimeSpan TimerInterval => TimeSpan.FromMilliseconds(100);

    #region Injects
    protected readonly ServerConnectionService ServerConnectionService;
    protected readonly ClientSettings Settings;
    protected readonly AudioService AudioService;
    protected readonly IAudioDeviceService AudioDeviceService;
    protected readonly IAudioPlayer AudioPlayer;
    protected readonly IAudioRecorder AudioRecorder;
    #endregion

    #region Members   
    protected BufferList<float> AudioBuffer = [];
    protected SemaphoreSlim Semaphore { get; init; } = new(1, 1);
    protected AudioSpectrogram AudioSpectrogram = new(windowSize: 320, stepSize: 160, poolingSize: 6);

    protected InferenceSession Session;
    protected Stopwatch Stopwatch = new();
    protected long CalculationDurationSum = 0;
    protected int CalculationNo = 0;

    protected bool StreamAudioDataToServer = false;
    protected int StreamAudioDataToServerCounter = 0;
    protected Guid CurrentAudioEventId = Guid.NewGuid();
    #endregion

    #region Constants
    protected const int SAMPLE_RATE = 16000;
    protected const int WAKE_WORD_WIDTH = 32000;
    protected const int STEP_WIDTH = (int)(SAMPLE_RATE * 0.5f); // every 0.5 seconds
    protected const int MAX_AUDIO_BUFFER_LENGTH = SAMPLE_RATE * 10; // 10 seconds
    protected const int MAX_AUDIO_STREAM_LENGTH = SAMPLE_RATE * 10; // 10 seconds
    protected const float WAKE_WORD_CONFIDENCE_LEVEL = 0.93f;
    #endregion

    public WakeWordListener(ServerConnectionService serverConnectionService,
        ClientSettings settings,
        AudioService audioService,
        IAudioDeviceService audioDeviceService,
        IAudioPlayer audioPlayer,
        IAudioRecorder audioRecorder,
        ILogger<WakeWordListener> logger,
        BaseErrorService baseErrorService)
        : base(logger, baseErrorService)
    {
        Settings = settings;
        ServerConnectionService = serverConnectionService;
        AudioService = audioService;
        AudioDeviceService = audioDeviceService;
        AudioPlayer = audioPlayer;
        AudioRecorder = audioRecorder;

        AudioRecorder.SampleRate = SAMPLE_RATE;
        AudioRecorder.OnDataAvailable += AudioRecorder_OnDataAvailable;

        var options = new SessionOptions();

        var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        ArgumentNullException.ThrowIfNull(executionDirectory);
        var wakeWordModelPath = Path.Combine(executionDirectory, "Resources", "WakeWord.onnx");
        Session = new InferenceSession(wakeWordModelPath, options);
    }

    protected override async Task OnTimerElapsedAsync(CancellationToken stoppingToken)
    {
        if (!Settings.ClientIsInitialized)
            return;

        if (!await AudioRecorder.CheckIsStillRunningAsync().ConfigureAwait(false))
        {
            await Task.Delay(10000, StopServiceToken).ConfigureAwait(false); // wait 10 seconds to delay the next try
            return;
        }

        if (StreamAudioDataToServer)
        {
            if (AudioBuffer.Count < SAMPLE_RATE * 0.25f)
                return;

            if (StreamAudioDataToServerCounter > MAX_AUDIO_STREAM_LENGTH)
            {
                StopAudioStreamToServer();
                return;
            }

            byte[] audioDataBytes;
            await Semaphore.WaitAsync(StopServiceToken).ConfigureAwait(false);
            try
            {
                StreamAudioDataToServerCounter += AudioBuffer.Count;
                audioDataBytes = AudioBuffer.ToByteArray(sizeof(float));
                AudioBuffer.Clear();
            }
            finally { Semaphore.Release(); }

            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.AudioData, CurrentAudioEventId, audioDataBytes)).ConfigureAwait(false);
            return;
        }

        if (AudioBuffer.Count > MAX_AUDIO_BUFFER_LENGTH) // As the calculation cannot take place in real time, we have to put some data in the bin to avoid taking up endless memory space.
        {
            var samplesToDelete = AudioBuffer.Count - MAX_AUDIO_BUFFER_LENGTH;

            await Semaphore.WaitAsync(StopServiceToken).ConfigureAwait(false);
            try { AudioBuffer.RemoveRange(0, samplesToDelete); } finally { Semaphore.Release(); }
            Logger.LogWarning("Removed {samplesToDelete} samples from audio buffer for the wake word detection because calculation cannot take place in real time", samplesToDelete);
        }

        if (AudioBuffer.Count < WAKE_WORD_WIDTH)
            return;

        Stopwatch.Restart();
        var currentAudioFrame = new float[WAKE_WORD_WIDTH];
        await Semaphore.WaitAsync(StopServiceToken).ConfigureAwait(false);
        try
        {
            AudioBuffer.CopyTo(0, currentAudioFrame, 0, WAKE_WORD_WIDTH);
            AudioBuffer.RemoveRange(0, STEP_WIDTH);
        }
        finally { Semaphore.Release(); }

        AudioService.NormalizeAudioData(currentAudioFrame);
        var spectrogram = AudioSpectrogram.GetSpectrogram(currentAudioFrame, useHannWindow: true);

        var input = new DenseTensor<float>([1, spectrogram.Length, spectrogram[0].Length, 1]);
        for (int i = 0; i < spectrogram.Length; i++)
            for (int y = 0; y < spectrogram[i].Length; y++)
                input[0, i, y, 0] = spectrogram[i][y];

        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, input.Buffer, [1, spectrogram.Length, spectrogram[0].Length, 1]);
        var inputs = new Dictionary<string, OrtValue>
        {
            { "input", inputOrtValue }
        };

        using var runOptions = new RunOptions();
        using var results = Session.Run(runOptions, inputs, Session.OutputNames);
        Stopwatch.Stop();
        CalculationDurationSum += Stopwatch.ElapsedMilliseconds;
        CalculationNo++;

        if (CalculationNo > 20)
        {
            var averageCalculationTime = Math.Round(CalculationDurationSum / (float)CalculationNo, 1);
            CalculationDurationSum = 0;
            CalculationNo = 0;
            Logger.LogInformation("Average calculation time for the wakeword: {AverageCalculationTime} ms", averageCalculationTime);
        }

        if (results.Count == 0)
            return;

        var isWakeWordProbability = results[0].GetTensorDataAsSpan<float>()[0];
        Debug.WriteLine(isWakeWordProbability);
        if (isWakeWordProbability > WAKE_WORD_CONFIDENCE_LEVEL)
        {
            Logger.LogInformation("Wake word detected with a probability of {WakeWordProbability}", isWakeWordProbability);

            CurrentAudioEventId = Guid.NewGuid();
            StreamAudioDataToServer = true;
            StreamAudioDataToServerCounter = 0;

            if (Settings.PlayRequestSound)
                await AudioPlayer.PlayAsync(SoundEffect.RequestSound);
        }
    }

    #region Audio Recorder

    private void AudioRecorder_OnDataAvailable(object? sender, Memory<float> floatSamples)
    {
        Semaphore.Wait(StopServiceToken);
        try
        {
            AudioBuffer.AddRangeSpan(floatSamples.Span);
        }
        finally { Semaphore.Release(); }
    }

    #endregion

    #region Methods

    public void StopAudioStreamToServer()
    {
        StreamAudioDataToServer = false;
    }

    #endregion
}
