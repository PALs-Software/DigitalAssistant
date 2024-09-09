
namespace DigitalAssistant.Client.Modules.Audio.Interfaces;

public interface IAudioRecorder : IDisposable
{
    event EventHandler<Memory<float>>? OnDataAvailable;
    int SampleRate {get;set;}

    Task<bool> CheckIsStillRunningAsync();
    Task<bool> StartAsync();
    Task<bool> StopAsync();
}
