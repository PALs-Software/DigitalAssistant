namespace DigitalAssistant.Client.Modules.General;

public static class SoftRestartService
{
    public static event EventHandler? OnSoftRestart;

    public static void ExecuteSoftRestart(object? sender = null)
    {
        OnSoftRestart?.Invoke(sender, EventArgs.Empty);
    }
}
