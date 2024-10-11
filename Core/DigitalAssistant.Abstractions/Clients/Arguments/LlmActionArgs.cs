using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Abstractions.Clients.Arguments;

public class LlmActionArgs : IClientActionArgs
{
    public string SystemPrompt { get; set; } = null!;
    public string UserPrompt { get; set; } = null!;

    public string? ForceStopOnToken { get; set; } = null;
    public int MaxLength { get; set; } = 512;
}