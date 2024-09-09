using BlazorBase.AudioRecorder.Services;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Ai.Asr.Services;
using Microsoft.AspNetCore.Components;
using static BlazorBase.AudioRecorder.Services.JSRawAudioRecorder;

namespace DigitalAssistant.Server.Modules.Ai.Asr.Components;

public partial class AsrAudioRecorder
{

    #region Parameter
    [Parameter] public EventCallback<string> OnNewAudioDataConverted { get; set; }
    [Parameter] public bool DebugModeEnabled { get; set; }
    #endregion

    #region Injects
    [Inject] protected AudioConverter AudioConverter { get; set; } = null!;
    [Inject] protected AudioService AudioService { get; set; } = null!;
    [Inject] protected AsrService AsrService { get; set; } = null!;
    [Inject] protected JSRawAudioRecorder JSRawAudioRecorder { get; set; } = null!;
    #endregion

    #region Properties
    public record DebugInformations(double RecordingTime, double AsrConversionTime);
    public DebugInformations? DebugInfos { get; protected set; } = null;
    #endregion

    #region Members
    protected bool IsRecording = false;
    protected long? JSRawAudioRecorderId = null;

    protected int WhishedSampleRate = 16000;
    protected int CurrentSampleRate = 16000;
    protected BufferList<float> Buffer = [];

    protected float Threshold = 0.009f;
    protected int IntervallCount = 0;

    protected DateTime RecordStart = DateTime.Now;
    protected DateTime RecordStop = DateTime.Now;
    protected DateTime AsrConversionStart = DateTime.Now;
    #endregion

    #region Init

    protected override void OnInitialized()
    {
        base.OnInitialized();
        JSRawAudioRecorder.OnReceiveData += JSRawAudioRecorder_OnReceiveData;
    }

    private void JSRawAudioRecorder_OnReceiveData(object? sender, OnReceiveDataArgs args)
    {
        if (!IsRecording)
            return;

        var currentRMS = AudioService.CalculateRms(args.Samples);
        if (currentRMS < Threshold && IntervallCount == 0)
            return;

        CurrentSampleRate = args.SampleRate;
        Buffer.AddRange(args.Samples);

        IntervallCount++;
        var speakerFinishedSpeaking = AudioService.SpeakerFinishedSpeaking(Buffer.AsSpan(), args.SampleRate, threshold: Threshold, maxDetectionDurationInSeconds: 15);

        if (!speakerFinishedSpeaking)
            return;

        IsRecording = false;
        Task.Run(StopAndProcessAudioDataAsync);
    }

    #endregion

    protected Task OnMicrophoneButtonClickedAsync()
    {
        if (IsRecording)
            return StopAndProcessAudioDataAsync();
        else
            return StartAudioAsync();
    }

    protected async Task StartAudioAsync()
    {
        if (DebugModeEnabled)
            RecordStart = DateTime.Now;

        Buffer.Clear();
        IsRecording = true;
        IntervallCount = 0;

        JSRawAudioRecorderId ??= await JSRawAudioRecorder.InitAsync();
        await JSRawAudioRecorder.StartAsync(JSRawAudioRecorderId.Value, WhishedSampleRate, WhishedSampleRate / 2);
    }

    protected async Task StopAndProcessAudioDataAsync()
    {
        if (JSRawAudioRecorderId == null)
            return;

        await JSRawAudioRecorder.StopAsync(JSRawAudioRecorderId.Value);
        if (Buffer.Count == 0)
            return;

        if (DebugModeEnabled)
        {
            RecordStop = DateTime.Now;
            AsrConversionStart = DateTime.Now;
        }

        var convertedText = await AsrService.ConvertSpeechToTextAsync(Buffer.ToArray(), CurrentSampleRate) ?? "";

        if (DebugModeEnabled)
        {
            var recordTime = (RecordStop - RecordStart).TotalMilliseconds;
            var asrConversionTime = (DateTime.Now - AsrConversionStart).TotalMilliseconds;
            DebugInfos = new(recordTime, asrConversionTime);
        }

        await InvokeAsync(async () =>
        {
            await OnNewAudioDataConverted.InvokeAsync(convertedText);
            StateHasChanged();
        });
    }
}
