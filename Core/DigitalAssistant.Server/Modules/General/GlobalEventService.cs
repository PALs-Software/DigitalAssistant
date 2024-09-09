namespace DigitalAssistant.Server.Modules.General;

public static class GlobalEventService
{
    public static event EventHandler<string>? OnClientConnected;
    public static event EventHandler<string>? OnClientDisconnected;

    public static void InvokeClientConnected(string clientName)
    {
        OnClientConnected?.Invoke(null, clientName);
    }

    public static void InvokeClientDisconnected(string clientName)
    {
        OnClientDisconnected?.Invoke(null, clientName);
    }
}
