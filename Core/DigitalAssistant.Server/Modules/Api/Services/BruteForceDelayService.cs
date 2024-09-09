namespace DigitalAssistant.Server.Modules.Api.Services;

public class BruteForceDelayService
{
    #region Members
    protected readonly List<int> Delays = [0, 0, 0, 100, 200, 400, 800, 1600, 3200, 6400];

    protected int RequestCount;
    protected DateTime LastRequest = DateTime.MinValue;
    protected DateTime LastPossibleResponse = DateTime.MinValue;
    protected TimeSpan ResetAfterTime = TimeSpan.FromMinutes(2);

    protected readonly object ThreadLock = new();
    #endregion

    public async Task DelayRequestAsync()
    {
        var delayTime = GetCurrentRequestDelay();
        await Task.Delay(delayTime);
    }

    public bool CanBeRemoved()
    {
        return DateTime.UtcNow - LastRequest > ResetAfterTime;
    }

    protected int GetCurrentRequestDelay()
    {
        lock (ThreadLock)
        {
            var currentDate = DateTime.UtcNow;
            if (currentDate - LastRequest > ResetAfterTime)
                RequestCount = 0;

            RequestCount++;
            LastRequest = currentDate;

            var currentDelay = RequestCount < Delays.Count ? Delays[RequestCount] : Delays.Last();
            LastPossibleResponse = LastPossibleResponse > currentDate ? LastPossibleResponse.AddMilliseconds(currentDelay) : LastPossibleResponse = currentDate.AddMilliseconds(currentDelay);

            return (int)(LastPossibleResponse - currentDate).TotalMilliseconds;
        }
    }

}
