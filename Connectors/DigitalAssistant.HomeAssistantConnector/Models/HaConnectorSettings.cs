using DigitalAssistant.Abstractions.Connectors;

namespace DigitalAssistant.HomeAssistantConnector.Models;

public class HaConnectorSettings : IConnectorSettings
{
    #region Properties

    public string? Url { get; set; }

    public string? AccessTokenEncrypted { get; set; }

    #endregion
}
