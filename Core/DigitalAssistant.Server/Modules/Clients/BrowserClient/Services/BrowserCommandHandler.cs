using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Server.Modules.Clients.BrowserClient.Commands;

namespace DigitalAssistant.Server.Modules.Clients.BrowserClient.Services;

public class BrowserCommandHandler(BrowserSystemCommandHandler browserSystemCommandHandler,
    BrowserMusicCommandHandler browserMusicCommandHandler,
    BrowserTimerCommandHandler browserTimerCommandHandler)
{
    #region Injects
    protected readonly BrowserSystemCommandHandler BrowserSystemCommandHandler = browserSystemCommandHandler;
    protected readonly BrowserMusicCommandHandler BrowserMusicCommandHandler = browserMusicCommandHandler;
    protected readonly BrowserTimerCommandHandler BrowserTimerCommandHandler = browserTimerCommandHandler;
    #endregion

    public Task<ClientActionResponse> ExecuteBrowserClientActionAsync(string language, IClient client, IClientActionArgs args)
    {
        return args.GetType().Name switch
        {
            nameof(SystemActionArgs) => BrowserSystemCommandHandler.ProcessSystemActionAsync((SystemActionArgs)args),
            nameof(MusicActionArgs) => BrowserMusicCommandHandler.ProcessMusicActionAsync((MusicActionArgs)args),
            nameof(TimerActionArgs) => BrowserTimerCommandHandler.ProcessTimerActionAsync((TimerActionArgs)args),
            _ => throw new NotImplementedException(),
        };
    }
}
