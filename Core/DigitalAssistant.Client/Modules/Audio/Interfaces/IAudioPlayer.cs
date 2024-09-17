
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Client.Modules.Audio.Enums;

namespace DigitalAssistant.Client.Modules.Audio.Interfaces;

public interface IAudioPlayer : IDisposable
{
    Task PlayAsync(byte[] audioData);
    Task PlayAsync(SoundEffect soundEffect);
    Task PlayAsync(string url);

    Task StopAsync(AudioType audioType, bool waitUntilStopped = false);
    Task PauseAsync(AudioType audioType);
    Task ResumeAsync(AudioType audioType);

    bool IsPlaying(AudioType audioType);
    bool IsPaused(AudioType audioType);

    void SetVolume(AudioType audioType, float volume);
}
