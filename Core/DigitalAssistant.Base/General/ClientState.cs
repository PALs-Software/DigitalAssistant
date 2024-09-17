using DigitalAssistant.Abstractions.Clients.Interfaces;

namespace DigitalAssistant.Base.General;

public class ClientState
{
    public List<IClientActionArgs> CurrentLongRunningActions { get; set; } = [];
    public Dictionary<Type, IClientActionArgs> LastLongRunningActions { get; set; } = [];


    public List<TClientActionArgs> GetCurrentLongRunningActions<TClientActionArgs>() where TClientActionArgs : IClientActionArgs
    {
        return CurrentLongRunningActions.Where(entry => entry is TClientActionArgs).Select(entry => (TClientActionArgs)entry).ToList();
    }

    public TClientActionArgs? GetLastLongRunningActionsIfExists<TClientActionArgs>() where TClientActionArgs : IClientActionArgs
    {
        LastLongRunningActions.TryGetValue(typeof(TClientActionArgs), out IClientActionArgs? value);
        return (TClientActionArgs?)value;
    }

    public void StopLongRunningActionIfExists<TClientActionArgs>() where TClientActionArgs : IClientActionArgs
    {
        var existingActions = CurrentLongRunningActions.Where(entry => entry is TClientActionArgs).ToList();
        foreach (var existingAction in existingActions)
            CurrentLongRunningActions.Remove(existingAction);

        if (existingActions.Count > 0)
            LastLongRunningActions[typeof(TClientActionArgs)] = existingActions.Last();
    }

    public void ReplaceLongRunningAction<TClientActionArgs>(TClientActionArgs action) where TClientActionArgs : IClientActionArgs
    {
        StopLongRunningActionIfExists<TClientActionArgs>();
        CurrentLongRunningActions.Add(action);
    }
}
