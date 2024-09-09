using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using DigitalAssistant.Server.Modules.General;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Clients.Components;

public class ClientStatusPopup
{
    #region Injects
    protected readonly IStringLocalizer<ClientStatusPopup> Localizer;
    protected readonly IMessageHandler MessageHandler;
    #endregion

    public ClientStatusPopup(IStringLocalizer<ClientStatusPopup> localizer, IMessageHandler messageHandler)
    {
        Localizer = localizer;
        MessageHandler = messageHandler;

        GlobalEventService.OnClientConnected += OnClientConnected;
        GlobalEventService.OnClientDisconnected += OnClientDisconnected;
    }

    private void OnClientConnected(object? sender, string clientName)
    {
        MessageHandler.ShowSnackbar(Localizer["Client \"{0}\" connected", clientName], messageType: MessageType.Information, millisecondsBeforeClose: 3000);
    }

    private void OnClientDisconnected(object? sender, string clientName)
    {
        MessageHandler.ShowSnackbar(Localizer["Client \"{0}\" disconnected", clientName], messageType: MessageType.Warning, millisecondsBeforeClose: 3000);
    }
}
