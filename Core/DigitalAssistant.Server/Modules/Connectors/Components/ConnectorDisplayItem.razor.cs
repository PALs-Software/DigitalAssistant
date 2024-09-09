using DigitalAssistant.Abstractions.Connectors;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Connectors.Components;

public partial class ConnectorDisplayItem
{
    #region Parameter
    [Parameter] public IConnector Connector { get; set; } = null!;
    [Parameter] public EventCallback<IConnector> OnEnableConnector { get; set; }
    [Parameter] public EventCallback<IConnector> OnDisableConnector { get; set; }
    #endregion

    #region Injects
    [Inject] public IStringLocalizer<ConnectorDisplayItem> Localizer { get; set; } = null!;
    #endregion


    protected Task EnableConnectorAsync()
    {
        return OnEnableConnector.InvokeAsync(Connector);
    }

    protected Task DisableConnectorAsync()
    {
        return OnDisableConnector.InvokeAsync(Connector);
    }
}
