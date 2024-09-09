namespace DigitalAssistant.Base.ClientServerConnection.MessageTransferModels;

public class ClientDevices
{
    public List<(string? Id, string Name)> OutputDevices { get; set; } = [];
    public List<(string? Id, string Name)> InputDevices { get; set; } = [];
}
