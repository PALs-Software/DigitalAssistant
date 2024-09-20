using BlazorBase.AudioRecorder.Services;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Services;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.AudioPlayer;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Services;
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Components;

public partial class AudioRecorderCommandProcessor
{
    #region Injects
    [Inject] protected CommandProcessor CommandProcessor { get; set; } = null!;
    [Inject] protected AudioConverter AudioConverter { get; set; } = null!;
    [Inject] protected TtsService TextToSpeechService { get; set; } = null!;
    [Inject] protected WebAudioPlayer ClientAudioPlayer { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    #endregion

    protected async Task OnNewAudioRecorderDataAsync(string? message)
    {
        message = message?.Trim();
        if (String.IsNullOrEmpty(message))
            return;

        var result = await CommandProcessor.ProcessUserCommandAsync(message, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, ClientBase.Browser, ServiceProvider);
        if (String.IsNullOrEmpty(result))
            return;

        var floats = await TextToSpeechService.ConvertTextToSpeechAsync(result);
        if (floats == null)
            return;

        var shorts = AudioConverter.ConvertFloatToShortSamples(floats.AsSpan());
        var bytes = AudioConverter.ConvertSamplesToWav(shorts, samplesPerSecond: TextToSpeechService.GetCurrentModelSampleRate() ?? 16000);
        await ClientAudioPlayer.PlayAudioAsync(bytes);
    }
}
