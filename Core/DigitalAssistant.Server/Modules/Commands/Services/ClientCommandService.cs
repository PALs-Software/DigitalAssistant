using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Server.Modules.AudioPlayer;
using DigitalAssistant.Server.Modules.Clients.Services;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class ClientCommandService(ClientInformationService clientInformationService,
    IServiceProvider serviceProvider,
    IStringLocalizer<ClientCommandService> localizer)
{
    #region Injects
    protected readonly ClientInformationService ClientInformationService = clientInformationService;
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly IStringLocalizer<ClientCommandService> Localizer = localizer;
    #endregion

    public Task<(bool Success, string? ErrorMessage)> ExecuteClientActionAsync(IClient client, IClientActionArgs args)
    {
        return args.GetType().Name switch
        {
            nameof(SystemActionArgs) => ProcessSystemActionArgsAsync(client, (SystemActionArgs)args),
            nameof(MusicActionArgs) => ProcessMusicActionArgsAsync(client, (MusicActionArgs)args),
            _ => throw new NotImplementedException(),
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> ProcessSystemActionArgsAsync(IClient client, SystemActionArgs args)
    {
        if (client.Type == ClientType.Browser)
        {
            throw new NotImplementedException();
        }

        var clientConnection = ClientInformationService.GetClientConnection(client.Id);
        if (clientConnection == null)
            return (false, Localizer["Client is currently not connected"]);

        var tcpActionMessage = TcpMessage.CreateActionMessage<SystemActionArgs>(TcpMessageActionType.SystemAction, args);
        var result = await clientConnection.SendMessageToClientAsync(tcpActionMessage)
                                           .ConfigureAwait(false);

        return (result.Success, result.Error?.Message);
    }

    public async Task<(bool Success, string? ErrorMessage)> ProcessMusicActionArgsAsync(IClient client, MusicActionArgs args)
    {
        if (string.IsNullOrEmpty(args.MusicStreamUrl))
            return (false, Localizer["No music stream url was provided"]);

        if (client.Type == ClientType.Browser)
        {
            try
            {
                var webAudioPlayer = ServiceProvider.GetRequiredService<WebAudioPlayer>();
                await webAudioPlayer.PlayAudioFromUrlAsync(args.MusicStreamUrl)
                           .ConfigureAwait(false);

                return (true, null);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        var clientConnection = ClientInformationService.GetClientConnection(client.Id);
        if (clientConnection == null)
            return (false, Localizer["Client is currently not connected"]);

        var tcpActionMessage = TcpMessage.CreateActionMessage<MusicActionArgs>(TcpMessageActionType.MusicAction, args);
        var result = await clientConnection.SendMessageToClientAsync(tcpActionMessage)
                                           .ConfigureAwait(false);

        return (result.Success, result.Error?.Message);
    }
}
