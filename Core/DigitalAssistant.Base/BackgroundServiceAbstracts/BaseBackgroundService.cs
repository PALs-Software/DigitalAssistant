using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DigitalAssistant.Base.General;

namespace DigitalAssistant.Base.BackgroundServiceAbstracts;

public abstract class BaseBackgroundService : BackgroundService
{
    #region Injects
    protected readonly ILogger Logger;
    protected readonly BaseErrorService BaseErrorService;
    #endregion

    public BaseBackgroundService(ILogger logger, BaseErrorService baseErrorService)
    {
        Logger = logger;
        BaseErrorService = baseErrorService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Start background service \"{Name}\"", Name);

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogWarning("Stop background service \"{Name}\"", Name);

        return base.StopAsync(cancellationToken);
    }

    public virtual string Name
    {
        get { return GetType().Name; }
    }
}
