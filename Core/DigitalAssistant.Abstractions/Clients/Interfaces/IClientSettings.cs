namespace DigitalAssistant.Abstractions.Clients.Interfaces;

public interface IClientSettings
{
    bool PlayRequestSound { get; set; }

    int VoiceAudioOutputSampleRate { get; set; }
    float OutputAudioVolume { get; set; }

    string? OutputDeviceId { get; set; }
    string? InputDeviceId { get; set; }
}
