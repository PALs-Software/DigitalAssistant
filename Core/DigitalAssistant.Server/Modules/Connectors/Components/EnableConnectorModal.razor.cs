using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.Services;
using Blazorise;
using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Server.Modules.Connectors.Models;
using DigitalAssistant.Server.Modules.Connectors.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Connectors.Components;

public partial class EnableConnectorModal
{
    #region Parameter
    [Parameter] public IConnector Connector { get; set; } = null!;
    [Parameter] public EventCallback Closed { get; set; }
    #endregion

    #region Injects
    [Inject] public IBaseDbContext DbContext { get; set; } = null!;
    [Inject] public IStringLocalizer<EnableConnectorModal> Localizer { get; set; } = null!;
    [Inject] public IMessageHandler MessageHandler { get; set; } = null!;
    [Inject] public ConnectorService ConnectorService { get; set; } = null!;
    [Inject] public BaseErrorHandler ErrorHandler { get; set; } = null!;
    #endregion

    #region Members
    protected Modal? Modal;
    #endregion

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
            Modal?.Show();
    }

    protected RenderFragment GetConnectorSetupRenderFragment() => builder =>
    {
        builder.OpenComponent(0, Connector.SetupComponentType);
        builder.AddAttribute(1, "Connector", Connector);
        builder.AddAttribute(2, "OnConnectorSetupFinished", EventCallback.Factory.Create<(bool Success, IConnectorSettings? Settings)>(this, ConnectorSetupFinishedAsync));
        builder.CloseComponent();
    };

    protected async Task ConnectorSetupFinishedAsync((bool Success, IConnectorSettings? Settings) args)
    {
        ulong messageId = 0;
        try
        {
            if (!args.Success || args.Settings == null || !Connector.Enabled)
                return;

            messageId = MessageHandler.ShowLoadingMessage(Localizer["SaveConnectorSettingsMessage"]);
            var connectorTypeName = Connector.GetType().AssemblyQualifiedName;
            if (String.IsNullOrEmpty(connectorTypeName))
                return;

            var oldConnectorSettings = await DbContext.WhereAsync<ConnectorSettings>(entry => entry.Type == connectorTypeName);
            if (oldConnectorSettings != null)
                await DbContext.RemoveRangeAsync(oldConnectorSettings);

            await DbContext.AddAsync(new ConnectorSettings()
            {
                Type = connectorTypeName,
                SettingsAsJson = JsonSerializer.Serialize(args.Settings, args.Settings.GetType()),
            });

            await DbContext.SaveChangesAsync();
            await ConnectorService.DiscoverAndUpdateDevicesAsync(Connector);
        }
        catch (Exception e)
        {
            MessageHandler.ShowMessage("UnexpectedError", ErrorHandler.PrepareExceptionErrorMessage(e), BlazorBase.MessageHandling.Enum.MessageType.Error);
        }
        finally
        {
            MessageHandler.CloseLoadingMessage(messageId);
            Modal?.Hide();
        }
    }
}
