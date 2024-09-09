using DigitalAssistant.Base;
using DigitalAssistant.Base.General;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Server.Modules.Connectors.Services;

namespace DigitalAssistant.Server.Modules.BackgroundJobs;

public class DiscoverAndUpdateDevices(ConnectorService connectorService, ILogger<DiscoverAndUpdateDevices> logger, BaseErrorService baseErrorService) : TimerBackgroundService(logger, baseErrorService)
{
    protected override TimeSpan TimerInterval => TimeSpan.FromHours(24);

    #region Injects
    protected readonly ConnectorService ConnectorService = connectorService;
    #endregion

    protected override Task OnTimerElapsedAsync()
    {
        return ConnectorService.DiscoverAndUpdateDevicesAsync();
    }

}
