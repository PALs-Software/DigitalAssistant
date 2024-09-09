namespace DigitalAssistant.Server.Modules.Clients.Models;

public class ClientConnectionSettings
{
    public int Port { get; set; }
    public CertificateSettings? Certificate { get; set; }
}
