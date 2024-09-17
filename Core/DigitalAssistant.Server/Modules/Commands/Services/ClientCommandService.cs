using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.Services;
using DigitalAssistant.Server.Modules.Clients.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class ClientCommandService(ClientInformationService clientInformationService,
    IStringLocalizer<ClientCommandService> localizer)
{
    #region Injects
    protected readonly ClientInformationService ClientInformationService = clientInformationService;
    protected readonly IStringLocalizer<ClientCommandService> Localizer = localizer;
    #endregion

    public Task<ClientActionResponse> ExecuteClientActionAsync(string language, IClient client, IClientActionArgs args, IServiceProvider serviceProvider)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            if (client.Type == ClientType.Browser)
                return serviceProvider.GetRequiredService<BrowserCommandHandler>().ExecuteBrowserClientActionAsync(language, client, args);

            return args.GetType().Name switch
            {
                nameof(SystemActionArgs) => ProcessActionArgsAsync(language, client, (SystemActionArgs)args, TcpMessageActionType.SystemAction),
                nameof(MusicActionArgs) => ProcessMusicActionArgsAsync(language, client, (MusicActionArgs)args),
                nameof(TimerActionArgs) => ProcessActionArgsAsync(language, client, (TimerActionArgs)args, TcpMessageActionType.TimerAction),
                _ => throw new NotImplementedException(),
            };
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    public Task<ClientActionResponse> ProcessMusicActionArgsAsync(string language, IClient client, MusicActionArgs args)
    {
        if (string.IsNullOrEmpty(args.MusicStreamUrl))
            return Task.FromResult(new ClientActionResponse(false, Localizer["NoMusicStreamUrlError"]));

        return ProcessActionArgsAsync(language, client, args, TcpMessageActionType.MusicAction);
    }

    public async Task<ClientActionResponse> ProcessActionArgsAsync<TActionArgs>(string language, IClient client, TActionArgs args, TcpMessageActionType actionType) where TActionArgs: IClientActionArgs
    {
        var clientConnection = ClientInformationService.GetClientConnection(client.Id);
        if (clientConnection == null)
            return new ClientActionResponse(false, Localizer["ClientNotConnectedError"]);

        var tcpActionMessage = TcpMessage.CreateActionMessage<TActionArgs>(actionType, args, language);
        var sendResponse = await clientConnection.SendMessageToClientAsync(tcpActionMessage).ConfigureAwait(false);
        if (!sendResponse.Success)
            return new ClientActionResponse(sendResponse.Success, sendResponse.Error?.Message);

        var re = await clientConnection.GetResponseDataAsync<ClientActionResponse>(tcpActionMessage.EventId, timeoutInMilliseconds: 15000).ConfigureAwait(false);


        return re ?? new ClientActionResponse(false, Localizer["ClientNotRespondError"]);
    }
}
