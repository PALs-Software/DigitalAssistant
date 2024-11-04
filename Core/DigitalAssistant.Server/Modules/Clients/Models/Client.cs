using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.Abstractions.General.Extensions;
using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Dashboards.Interfaces;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.ClientServerConnection.MessageTransferModels;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Services;
using DigitalAssistant.Server.Modules.Groups.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Clients.Models;

[Route("/Clients")]
[Authorize(Roles = "Admin")]
public partial class Client : ClientBase, IClient, IClientSettings, IDashboardEntry
{
    #region Properties

    [StringLength(128)]
    public string TokenHash { get; set; } = null!;

    [Visible(DisplayOrder = 500)]
    [Required]
    [PresentationDataType(PresentationDataType.DateTime)]
    public DateTime ValidUntil { get; set; }

    [Visible(DisplayOrder = 400)]
    [Editable(false)]
    public bool HasBeenInitialized { get; set; }

    [Visible(DisplayOrder = 500, HideInGUITypes = [GUIType.ListPart])]
    [ForeignKey(nameof(Group))]
    public virtual Guid? GroupId { get; set; } = null;
    public virtual Group? Group { get; set; } = null;

    [Visible(DisplayOrder = 600)]
    public bool ShowInDashboard { get; set; } = true;

    [Visible(DisplayOrder = 700)]
    public int DashboardOrder { get; set; }

    #region Client Settings
    public bool ClientNeedSettingsUpdate { get; set; }

    [Visible(DisplayGroup = "Settings", DisplayGroupOrder = 100, DisplayOrder = 100, HideInGUITypes = [GUIType.List])]
    public bool PlayRequestSound { get; set; } = true;

    public int VoiceAudioOutputSampleRate { get; set; } = 22050;

    [Visible(DisplayGroup = "Settings", DisplayOrder = 200, HideInGUITypes = [GUIType.List])]
    public float OutputAudioVolume { get; set; } = 0.5f;

    [Visible(DisplayGroup = "Settings", DisplayOrder = 300, HideInGUITypes = [GUIType.List])]
    [UseCustomLookupData(nameof(GetDevices))]
    public string? OutputDeviceId { get; set; }

    [Visible(DisplayGroup = "Settings", DisplayOrder = 400, HideInGUITypes = [GUIType.List])]
    [UseCustomLookupData(nameof(GetDevices))]
    public string? InputDeviceId { get; set; }

    #endregion

    [Timestamp]
    public byte[]? SqlRowVersion { get; set; }

    #region Not Mapped

    [Visible(DisplayOrder = 300, HideInGUITypes = [GUIType.List])]
    [Editable(false)]
    [NotMapped]
    public bool Online { get; set; }

    [NotMapped]
    public Guid? LastProcessedAudioMessageEventId { get; set; }

    [NotMapped]
    public ClientDevices ClientDevices { get; set; } = new();

    #endregion

    #endregion

    #region CRUD

    public override Task OnCreateNewEntryInstance(OnCreateNewEntryInstanceArgs args)
    {
        if (args.Model is Client client && client.ValidUntil == DateTime.MinValue)
            client.ValidUntil = DateTime.Now.Date.AddYears(30);

        return base.OnCreateNewEntryInstance(args);
    }

    public override Task OnAfterAddEntry(OnAfterAddEntryArgs args)
    {
        var token = TokenService.GenerateRandomToken(128); // 128 characters long - 1024 bit strong access token
        TokenHash = token.CreateSHA512Hash();

        args.EventServices.ServiceProvider.GetRequiredService<IMessageHandler>().ShowMessage(
            args.EventServices.Localizer["NewClient"],
            args.EventServices.Localizer["NewClientAddedMessage", token]
        );

        return base.OnAfterAddEntry(args);
    }

    public override async Task OnAfterCardSaveChanges(OnAfterCardSaveChangesArgs args)
    {
        await base.OnAfterCardSaveChanges(args);

        await UpdateSettingsOnClientAsync(args.EventServices, skipSettingClientNeedSettingsUpdateOnFailure: false);
    }

    public async Task<bool> UpdateSettingsOnClientAsync(EventServices eventServices, bool skipSettingClientNeedSettingsUpdateOnFailure = false)
    {
        if (!HasBeenInitialized)
            return false;

        var success = await HandleUpdateSettingsOnClientAsync(eventServices, skipSettingClientNeedSettingsUpdateOnFailure);
        await UpdateClientCacheAsync(eventServices.DbContext);
        return success;
    }

    protected async Task<bool> HandleUpdateSettingsOnClientAsync(EventServices eventServices, bool skipSettingClientNeedSettingsUpdateOnFailure = false)
    {
        var clientInformationService = eventServices.ServiceProvider.GetRequiredService<ClientInformationService>();
        var connection = clientInformationService.GetClientConnection(Id);
        if (connection == null || !connection.ClientIsAuthenticated)
        {
            await SetClientNeedSettingsUpdateAsync(eventServices);
            ShowFailedUpdateSettingsOnClientMessage(eventServices);
            return false;
        }

        var eventId = Guid.NewGuid();
        var clientSettings = new ClientSettings();
        this.TransferPropertiesTo(clientSettings);

        var json = JsonSerializer.Serialize(clientSettings);
        var bytes = Encoding.UTF8.GetBytes(json);

        await connection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, eventId, bytes));
        var success = await connection.GetResponseDataAsync<bool>(eventId, timeoutInMilliseconds: 10000);
        if (success)
        {
            var messageHandler = eventServices.ServiceProvider.GetRequiredService<IMessageHandler>();
            messageHandler.ShowSnackbar(eventServices.Localizer["UpdateSuccessMessage"], messageType: MessageType.Information, millisecondsBeforeClose: 3000);
            return true;
        }

        if (!skipSettingClientNeedSettingsUpdateOnFailure)
            await SetClientNeedSettingsUpdateAsync(eventServices);
        ShowFailedUpdateSettingsOnClientMessage(eventServices);

        return false;
    }

    protected void ShowFailedUpdateSettingsOnClientMessage(EventServices eventServices)
    {
        var messageHandler = eventServices.ServiceProvider.GetRequiredService<IMessageHandler>();
        messageHandler.ShowSnackbar(eventServices.Localizer["UpdateFailedMessage"], messageType: MessageType.Error, millisecondsBeforeClose: 4000);
    }

    protected async Task SetClientNeedSettingsUpdateAsync(EventServices eventServices)
    {
        ClientNeedSettingsUpdate = true;
        await eventServices.DbContext.SaveChangesAsync();
    }

    public override Task OnAfterDbContextDeletedEntry(OnAfterDbContextDeletedEntryArgs args)
    {
        Cache.ClientCache.Clients.Remove(TokenHash, out _);
        return base.OnAfterDbContextDeletedEntry(args);
    }

    #endregion

    #region Audio Devices Custom Lookup Data

    public override async Task OnShowEntry(OnShowEntryArgs args)
    {
        await base.OnShowEntry(args);

        if (args.GuiType != GUIType.Card)
            return;

        var clientInformationService = args.EventServices.ServiceProvider.GetRequiredService<ClientInformationService>();
        var connection = clientInformationService.GetClientConnection(Id);
        Online = connection != null && connection.ClientIsAuthenticated;
        if (connection == null || !connection.ClientIsAuthenticated)
            return;

        var eventId = Guid.NewGuid();
        await connection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.TransferAudioDevices, eventId, []));
        var clientDevices = await connection.GetResponseDataAsync<ClientDevices>(eventId, timeoutInMilliseconds: 5000);
        if (clientDevices == null)
            return;

        ClientDevices = clientDevices;
    }

    public static Task GetDevices(PropertyInfo propertyInfo, IBaseModel cardModel, List<KeyValuePair<string?, string>> lookupData, EventServices eventServices)
    {
        if (cardModel is not Client client)
            return Task.CompletedTask;

        var devices = propertyInfo.Name == nameof(OutputDeviceId) ? client.ClientDevices.OutputDevices : client.ClientDevices.InputDevices;

        lookupData.Add(new KeyValuePair<string?, string>(null, String.Empty));
        foreach (var device in devices)
            lookupData.Add(new KeyValuePair<string?, string>(device.Id, device.Name));

        return Task.CompletedTask;
    }
    #endregion

    #region MISC

    public async Task UpdateClientCacheAsync(IBaseDbContext dbContext)
    {
        var untrackedClient = await dbContext.FirstAsync<Client>(entry => entry.Id == Id, asNoTracking: true);
        Cache.ClientCache.Clients[TokenHash] = untrackedClient;
    }

    #endregion
}
