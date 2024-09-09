using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Client.Modules.State;

public class ClientState
{
    public List<IClientActionArgs> CurrentLongRunningActions { get; set; } = [];
    public IClientActionArgs? LastLongRunningAction { get; set; }

    public void StopLongRunningActionIfExists<TClientActionArgs>() where TClientActionArgs : IClientActionArgs
    {
        var existingActions = CurrentLongRunningActions.Where(entry => entry is TClientActionArgs).ToList();
        foreach (var existingAction in existingActions)
            CurrentLongRunningActions.Remove(existingAction);

        if (existingActions.Count > 0)
            LastLongRunningAction = existingActions.Last();
    }

    public void ReplaceLongRunningAction<TClientActionArgs>(TClientActionArgs action) where TClientActionArgs : IClientActionArgs
    {
        StopLongRunningActionIfExists<TClientActionArgs>();
        CurrentLongRunningActions.Add(action);
    }
}
