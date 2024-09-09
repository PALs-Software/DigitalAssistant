using System.Security.Cryptography.X509Certificates;

namespace DigitalAssistant.Server.Modules.Clients.Models;

public class CertificateSettings
{
    public StoreLocation? Location { get; set; }
    public string? Store { get; set; }
    public string? Subject { get; set; }
    public string? Path { get; set; }
    public string? KeyPath { get; set; }
    public string? Password { get; set; }
}
