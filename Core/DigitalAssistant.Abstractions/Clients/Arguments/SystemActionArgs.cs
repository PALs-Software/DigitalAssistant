using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Abstractions.Clients.Arguments;

public class SystemActionArgs : IClientActionArgs
{
    public bool? StopCurrentAction { get; set; }
    public bool? PauseCurrentAction { get; set; }
    public bool? ContinueLastAction { get; set; }
    public bool? Next { get; set; }
    public bool? Previous { get; set; }
    public bool? IncreaseVolume { get; set; }
    public bool? DecreaseVolume { get; set; }
    public float? SetVolume { get; set; }
}
