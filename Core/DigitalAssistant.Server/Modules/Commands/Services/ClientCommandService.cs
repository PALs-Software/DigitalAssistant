using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Server.Modules.Ai.Llm.Enums;
using DigitalAssistant.Server.Modules.Ai.Llm.Services;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.Services;
using DigitalAssistant.Server.Modules.Clients.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Services;

public class ClientCommandService(ClientInformationService clientInformationService,
    LlmService llmService,
    IStringLocalizer<ClientCommandService> localizer)
{
    #region Injects
    protected readonly ClientInformationService ClientInformationService = clientInformationService;
    protected readonly LlmService LlmService = llmService;
    protected readonly IStringLocalizer<ClientCommandService> Localizer = localizer;
    #endregion

    public Task<ClientActionResponse> ExecuteClientActionAsync(string language, IClient client, IClientActionArgs args, IServiceProvider serviceProvider)
    {
        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var argType = args.GetType().Name;

            if (client.Type == ClientType.Browser && argType != nameof(LlmActionArgs))
                return serviceProvider.GetRequiredService<BrowserCommandHandler>().ExecuteBrowserClientActionAsync(language, client, args);

            return args.GetType().Name switch
            {
                nameof(SystemActionArgs) => ProcessActionArgsAsync(language, client, (SystemActionArgs)args, TcpMessageActionType.SystemAction),
                nameof(MusicActionArgs) => ProcessMusicActionArgsAsync(language, client, (MusicActionArgs)args),
                nameof(TimerActionArgs) => ProcessActionArgsAsync(language, client, (TimerActionArgs)args, TcpMessageActionType.TimerAction),
                nameof(LlmActionArgs) => ProcessLlmActionArgsAsync(language, client, (LlmActionArgs)args),
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
        if (String.IsNullOrEmpty(args.MusicStreamUrl))
            return Task.FromResult(new ClientActionResponse(false, Localizer["NoMusicStreamUrlError"]));

        return ProcessActionArgsAsync(language, client, args, TcpMessageActionType.MusicAction);
    }

    public async Task<ClientActionResponse> ProcessLlmActionArgsAsync(string language, IClient client, LlmActionArgs args)
    {
        if (String.IsNullOrEmpty(args.SystemPrompt) || String.IsNullOrEmpty(args.UserPrompt))
            return new ClientActionResponse(false, Localizer["NoLlmPromptError"]);

        if (Cache.SetupCache.Setup?.LlmModel == LlmModels.Disabled)
            return new ClientActionResponse(false, Localizer["LlmDisabledError"]);

        var answer = await LlmService.GenerateAnswerAsync(args.SystemPrompt, args.UserPrompt, args.ForceStopOnToken, args.MaxLength);

        return new ClientActionResponse(true, answer?.Trim());
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

        var response = await clientConnection.GetResponseDataAsync<ClientActionResponse>(tcpActionMessage.EventId, timeoutInMilliseconds: 15000).ConfigureAwait(false);

        return response ?? new ClientActionResponse(false, Localizer["ClientNotRespondError"]);
    }
}
