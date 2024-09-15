using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Base.ClientServerConnection;

public class ClientSettings : IClientSettings
{
    public bool ClientIsInitialized { get; set; }

    public bool PlayRequestSound { get; set; } = true;

    public int VoiceAudioOutputSampleRate { get; set; } = 22050;
    public float OutputAudioVolume { get; set; } = 0.5f;

    public string? OutputDeviceId { get; set; }
    public string? InputDeviceId { get; set; }
}