using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Server.Modules.Connectors.Models;
using DigitalAssistant.Server.Modules.Connectors.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Connectors.Pages;

public partial class ConnectorList
{
    #region Injects
    [Inject] protected IStringLocalizer<ConnectorList> Localizer { get; set; } = null!;
    [Inject] protected ConnectorService ConnectorService { get; set; } = null!;
    [Inject] public IBaseDbContext DbContext { get; set; } = null!;
    #endregion

    #region Members
    protected List<IConnector> EnabledConnectors = [];

    protected List<IConnector> DisabledConnectors = [];
    protected bool[] AccordionCollapseState = [true, true];

    protected IConnector? ConnectorToEnable;
    protected bool ShowEnableConnectorModal = false;
    #endregion

    protected override Task OnInitializedAsync()
    {
        var connectors = ConnectorService.GetConnectors();
        EnabledConnectors = connectors.Where(entry => entry.Enabled).ToList();
        DisabledConnectors = connectors.Where(entry => !entry.Enabled).ToList();

        return base.OnInitializedAsync();
    }

    protected Task EnableConnectorAsync(IConnector connector)
    {
        ConnectorToEnable = connector;
        ShowEnableConnectorModal = true;
        return Task.CompletedTask;
    }

    protected async Task DisableConnectorAsync(IConnector connector)
    {
        var connectorTypeName = connector.GetType().AssemblyQualifiedName;
        if (String.IsNullOrEmpty(connectorTypeName))
            return;

        await connector.DisableConnectorAsync();

        var connectorSettings = await DbContext.WhereAsync<ConnectorSettings>(entry => entry.Type == connectorTypeName);
        if (connectorSettings != null) {
            await DbContext.RemoveRangeAsync(connectorSettings);
            await DbContext.SaveChangesAsync();
        }

        RefreshConnectorState(connector);
    }

    protected void EnableConnectorModalClosed()
    {
        if (ConnectorToEnable != null)
            RefreshConnectorState(ConnectorToEnable);

        ShowEnableConnectorModal = false;
        ConnectorToEnable = null;
    }

    protected void RefreshConnectorState(IConnector connector)
    {
        EnabledConnectors.Remove(connector);
        DisabledConnectors.Remove(connector);
        if (connector.Enabled)
            EnabledConnectors.Add(connector);
        else
            DisabledConnectors.Add(connector);
    }
}
