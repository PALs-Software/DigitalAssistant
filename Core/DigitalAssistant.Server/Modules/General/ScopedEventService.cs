namespace DigitalAssistant.Server.Modules.General;

public class ScopedEventService
{
    public event EventHandler? OnProfileImageChanged;

    public void InvokeProfileImageChanged()
    {
        OnProfileImageChanged?.Invoke(null, EventArgs.Empty);
    }
}
