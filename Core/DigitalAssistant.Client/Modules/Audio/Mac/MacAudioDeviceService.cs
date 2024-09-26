using DigitalAssistant.Client.Modules.Audio.Interfaces;

namespace DigitalAssistant.Client.Modules.Audio.Mac;

public partial class MacAudioDeviceService : IAudioDeviceService
{
    public List<(string? Id, string Name)> GetOutputDevices()
    {
        return [(null, "System")];
    }

    public List<(string? Id, string Name)> GetInputDevices()
    {
        return [(null, "System")];
    }
}
