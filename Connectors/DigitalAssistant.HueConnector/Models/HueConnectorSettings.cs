using BlazorBase.Abstractions.CRUD.Attributes;
using DigitalAssistant.Abstractions.Connectors;

namespace DigitalAssistant.HueConnector.Models;

public class HueConnectorSettings : IConnectorSettings
{
    #region Properties
    [Visible]
    public string? Ip { get; set; }

    [Visible]
    public int Port { get; set; }

    public string? AccessKeyEncrypted { get; set; }

    public string? ClientKeyEncrypted { get; set; }

    #endregion
}
