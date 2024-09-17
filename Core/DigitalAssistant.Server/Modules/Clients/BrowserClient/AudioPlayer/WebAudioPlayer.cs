using DigitalAssistant.Base.Audio;
using Microsoft.JSInterop;

namespace DigitalAssistant.Server.Modules.Clients.BrowserClient.AudioPlayer;

public class WebAudioPlayer(IJSRuntime jsRuntime)
{
    #region Injects
    protected readonly IJSRuntime JSRuntime = jsRuntime;
    #endregion

    public async ValueTask PlayAudioAsync(byte[] audioData)
    {
        var memoryStream = new MemoryStream(audioData);
        using var dotNetStreamReference = new DotNetStreamReference(stream: memoryStream);
        await JSRuntime.InvokeVoidAsync("DA.PlayAudioFileStreamAsync", dotNetStreamReference);
        memoryStream.Dispose();
    }

    public ValueTask PlayAudioFromUrlAsync(string url)
    {
        return JSRuntime.InvokeVoidAsync("DA.PlayAudioFromUrl", url);
    }

    public ValueTask PlayAudioAsync(SoundEffect soundEffect)
    {
        return PlayAudioFromUrlAsync($"/audio/{soundEffect}.mp3");
    }

    public ValueTask PauseAudioAsync()
    {
        return JSRuntime.InvokeVoidAsync("DA.PauseAudio");
    }

    public ValueTask ResumeAudioAsync()
    {
        return JSRuntime.InvokeVoidAsync("DA.ResumeAudio");
    }
}
