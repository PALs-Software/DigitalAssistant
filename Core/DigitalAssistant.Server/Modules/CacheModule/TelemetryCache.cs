using DigitalAssistant.Server.Modules.Telemetry.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.CacheModule;

public class TelemetryCache
{
    #region API

    public ConcurrentDictionary<string, ApiTelemetryEntry> ApiTelemetryEntries = new();

    public void IncreaseApiTelemetryCount(ActionDescriptor actionDescriptor)
    {
        IncreaseApiTelemetryCount(actionDescriptor.AttributeRouteInfo?.Template?.Replace("Api/", "") ?? String.Empty);
    }

    public void IncreaseApiTelemetryCount(string actionName)
    {
        if (!ApiTelemetryEntries.TryGetValue(actionName, out ApiTelemetryEntry? telemetryEntry))
        {
            telemetryEntry = new ApiTelemetryEntry() { Name = actionName, LastRequest = DateTime.MinValue, LastErrorRequest = DateTime.MinValue };
            ApiTelemetryEntries.TryAdd(telemetryEntry.Name, telemetryEntry);
        }

        telemetryEntry.Count++;
        telemetryEntry.LastRequest = DateTime.Now;
    }

    public void IncreaseApiTelemetryErrorCount(ActionDescriptor actionDescriptor, Exception exception)
    {        
        IncreaseApiTelemetryErrorCount(actionDescriptor.AttributeRouteInfo?.Template?.Replace("Api/", "") ?? String.Empty, exception);
    }

    public void IncreaseApiTelemetryErrorCount(string actionName, Exception exception)
    {
        if (!ApiTelemetryEntries.TryGetValue(actionName, out ApiTelemetryEntry? telemetryEntry))
        {
            telemetryEntry = new ApiTelemetryEntry() { Name = actionName, LastRequest = DateTime.MinValue, LastErrorRequest = DateTime.MinValue };
            ApiTelemetryEntries.TryAdd(telemetryEntry.Name, telemetryEntry);
        }

        telemetryEntry.ErrorCount++;
        telemetryEntry.LastErrorRequest = DateTime.Now;
        telemetryEntry.LastErrorMessage = PrepareExceptionErrorMessage(exception);
    }

    private string PrepareExceptionErrorMessage(Exception e)
    {
        if (e.InnerException == null)
            return e.Message;

        return e.Message + Environment.NewLine + Environment.NewLine + "Inner Exception:" + PrepareExceptionErrorMessage(e.InnerException);
    }
    #endregion
}
