using Microsoft.Extensions.Logging;
using DigitalAssistant.Base.General;

namespace DigitalAssistant.Base.BackgroundServiceAbstracts;

public abstract class TimerBackgroundService : BaseBackgroundService
{
    #region Properties
    protected abstract TimeSpan TimerInterval { get; }
    #endregion

    #region Member
    protected readonly TimeSpan ZeroTimeSpan = TimeSpan.FromSeconds(0);
    protected PeriodicTimer PeriodicTimer;
    protected CancellationToken StopServiceToken;
    #endregion

    protected TimerBackgroundService(ILogger logger, BaseErrorService baseErrorService) : base(logger, baseErrorService)
    {
        PeriodicTimer = new PeriodicTimer(TimerInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            StopServiceToken = stoppingToken;
            do
            {
                try
                {
                    await OnTimerElapsedAsync().ConfigureAwait(false);
                }
                catch (Exception) { }
            } while (!stoppingToken.IsCancellationRequested && await PeriodicTimer.WaitForNextTickAsync(stoppingToken));
        }
        catch (Exception e)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(e, "Unexpected error in the background service \"{BackgroundServiceName}\": {ExceptionMessage}", Name, BaseErrorService.PrepareExceptionErrorMessage(e));
        }
        finally
        {
            PeriodicTimer.Dispose();
            Logger.LogWarning("Stopped background service \"{Name}\"", Name);
        }
    }

    protected abstract Task OnTimerElapsedAsync();

}