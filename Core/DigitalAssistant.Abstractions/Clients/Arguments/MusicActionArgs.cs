using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Abstractions.Clients.Arguments;

public class MusicActionArgs : IClientActionArgs
{
    public string? MusicStreamUrl { get; set; }
}