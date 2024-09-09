
namespace DigitalAssistant.Client.Modules.Audio.Interfaces;

public interface IAudioDeviceService
{ 
    List<(string? Id, string Name)> GetOutputDevices();

    List<(string? Id, string Name)> GetInputDevices();
}
