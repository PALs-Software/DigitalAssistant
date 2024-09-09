﻿using BlazorBase.AudioRecorder.Services;
using DigitalAssistant.Server.Modules.AudioPlayer;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using TextToSpeech;

namespace DigitalAssistant.Server.Modules.Commands.Components;

public partial class AudioRecorderCommandProcessor
{
    #region Injects
    [Inject] protected CommandProcessor CommandProcessor { get; set; } = null!;
    [Inject] protected AudioConverter AudioConverter { get; set; } = null!;
    [Inject] protected TextToSpeechService TextToSpeechService { get; set; } = null!;
    [Inject] protected WebAudioPlayer ClientAudioPlayer { get; set; } = null!;
    #endregion

    protected async Task OnNewAudioRecorderDataAsync(string? message)
    {
        message = message?.Trim();
        if (String.IsNullOrEmpty(message))
            return;

        var result = await CommandProcessor.ProcessUserCommandAsync(message, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, ClientBase.Browser);
        if (result == null)
            return;

        var floats = await TextToSpeechService.ConvertTextToSpeechAsync(result);
        if (floats == null)
            return;

        var shorts = AudioConverter.ConvertFloatToShortSamples(floats.AsSpan());
        var bytes = AudioConverter.ConvertSamplesToWav(shorts, samplesPerSecond: TextToSpeechService.GetCurrentModelSampleRate() ?? 16000);
        await ClientAudioPlayer.PlayAudioAsync(bytes);
    }
}
