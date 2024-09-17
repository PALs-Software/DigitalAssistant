using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Abstractions.Clients.Arguments;

public class TimerActionArgs : IClientActionArgs
{
    public string? Name { get; set; }
    public bool? SetTimer { get; set; }
    public bool? GetTimer { get; set; }
    public bool? DeleteTimer { get; set; }
    public TimeSpan? Duration { get; set; }

    public Task? TimerTask { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    public DateTime? TimerEnd { get; set; }
}