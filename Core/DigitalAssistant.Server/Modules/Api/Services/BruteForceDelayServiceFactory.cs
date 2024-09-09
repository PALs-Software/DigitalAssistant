using System.Collections.Concurrent;
using System.Net;

namespace DigitalAssistant.Server.Modules.Api.Services;

public class BruteForceDelayServiceFactory()
{
    #region Members
    protected ConcurrentDictionary<string, BruteForceDelayService> Services { get; } = new();
    protected DateTime LastCleanUp = DateTime.MinValue;
    protected readonly TimeSpan CleanUpIntervall = TimeSpan.FromMinutes(5);
    protected readonly object ThreadLock = new();
    #endregion

    public BruteForceDelayService GetOrCreate(IPAddress? ipAddress)
    {
        lock (ThreadLock)
        {
            if (DateTime.UtcNow - LastCleanUp > CleanUpIntervall)
            {
                LastCleanUp = DateTime.UtcNow;

                var servicesThatCanBeRemoved = new List<string>();
                foreach (var keyValuePair in Services)
                    if (keyValuePair.Value.CanBeRemoved())
                        servicesThatCanBeRemoved.Add(keyValuePair.Key);

                foreach (var serviceKey in servicesThatCanBeRemoved)
                    Services.TryRemove(serviceKey, out _);
            }
        }

        return Services.GetOrAdd(ipAddress?.ToString() ?? String.Empty, key => { return new BruteForceDelayService(); });
    }
}
