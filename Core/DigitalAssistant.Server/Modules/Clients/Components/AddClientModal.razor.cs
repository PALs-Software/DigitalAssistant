using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.CRUD.Components.Card;
using BlazorBase.CRUD.Components.List;
using BlazorBase.CRUD.Models;
using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using Blazorise;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.Extensions;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Clients.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Text;

namespace DigitalAssistant.Server.Modules.Clients.Components;

public partial class AddClientModal : IActionComponent
{
    #region Parameter

    [Parameter] public ActionComponentArgs Args { get; set; } = null!;
    [Parameter] public EventCallback ComponentCanBeRemoved { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public string? ButtonText { get; set; }

    #endregion

    #region Injects
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected IStringLocalizer<AddClientModal> Localizer { get; set; } = null!;
    [Inject] protected IMessageHandler MessageHandler { get; set; } = null!;
    [Inject] protected ClientInformationService ClientInformationService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    #endregion

    #region Member

    protected Modal? Modal;
    protected BaseModalCard<Client>? ClientModalCard;
    protected List<ClientConnection> AvailableClients = [];

    protected bool ShowClientCard = false;
    protected ExplainText? AddClientManuallyExplainText;
    protected Guid? SelectedClientId;
    #endregion

    protected override Task OnInitializedAsync()
    {
        AvailableClients = ClientInformationService.GetAvailableClientsToSetup();
        AddClientManuallyExplainText = new ExplainText(Localizer["AddClientManuallyExplainText"], ExplainTextLocation.Top);

        return base.OnInitializedAsync();
    }

    protected async Task OnAddNewClientManuallyButtonClickedAsync()
    {
        if (ClientModalCard == null)
            return;

        ShowClientCard = true;
        await ClientModalCard.ShowModalAsync(addingMode: true);
    }

    protected async Task OnAddSelectedClientButtonClickedAsync()
    {
        if (SelectedClientId == null)
            return;

        var selectedClientConnection = AvailableClients.FirstOrDefault(entry => entry.Client?.Id == SelectedClientId);
        if (selectedClientConnection == null || selectedClientConnection.Client == null)
            return;

        var loadingMessageId = MessageHandler.ShowLoadingMessage(Localizer["LoadingMessage"]);
        try
        {
            var token = TokenService.GenerateRandomToken(128); // 128 characters long - 1024 bit strong access token
            var client = new Client()
            {
                Name = selectedClientConnection.Client.Name,
                ValidUntil = DateTime.Now.Date.AddYears(30),
                TokenHash = token.CreateSHA512Hash()
            };
            await DbContext.AddAsync(client);
            await DbContext.SaveChangesAsync();

            var eventId = Guid.NewGuid();
            await selectedClientConnection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.SetupClientWithServer, eventId, Encoding.UTF8.GetBytes(token)));
            var response = await selectedClientConnection.GetResponseDataAsync<byte>(eventId, timeoutInMilliseconds: 5000);
            if (response == 0)
            {
                await DbContext.RemoveAsync(client);
                await DbContext.SaveChangesAsync();

                MessageHandler.ShowMessage(Localizer["AddedClientErrorTitle"], Localizer["AddedClientErrorMessage"], MessageType.Error);
                return;
            }

            client.Type = (ClientType)response - 1;
            client.HasBeenInitialized = true;
            await DbContext.SaveChangesAsync();

            var oldClientId = selectedClientConnection.Client.Id;
            var cachedClient = await DbContext.FirstAsync<Client>(entry => entry.Id == client.Id, asNoTracking: true);
            Cache.ClientCache.Clients[client.TokenHash] = cachedClient;
            selectedClientConnection.Client = cachedClient;
            selectedClientConnection.ClientIsAuthenticated = true;
            selectedClientConnection.IsAvailableClientForSetup = false;

            ClientInformationService.RemoveClient(oldClientId, selectedClientConnection);
            ClientInformationService.AddClient(cachedClient.Id, selectedClientConnection);

            MessageHandler.ShowMessage(Localizer["AddedClientSuccessTitle"], Localizer["AddedClientSuccessMessage", cachedClient.Name], MessageType.Success);
            NavigationManager.NavigateTo($"/Clients?Id={cachedClient.Id}");
            Modal?.Hide();
        }
        finally
        {
            MessageHandler.CloseLoadingMessage(loadingMessageId);
        }
    }

    private Task OnModalClosing(ModalClosingEventArgs args)
    {
        return ComponentCanBeRemoved.InvokeAsync();
    }

    private async Task OnCardClosedAsync()
    {
        if (Args.Source != null)
            await ((BaseList<Client>)Args.Source).RefreshDataAsync();

        await ComponentCanBeRemoved.InvokeAsync();
    }
}