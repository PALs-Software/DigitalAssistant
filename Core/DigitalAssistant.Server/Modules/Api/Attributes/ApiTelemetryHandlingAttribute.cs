using DigitalAssistant.Server.Modules.CacheModule;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DigitalAssistant.Server.Modules.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiTelemetryHandlingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Cache.TelemetryCache.IncreaseApiTelemetryCount(context.ActionDescriptor);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception == null)
            return;

        Cache.TelemetryCache.IncreaseApiTelemetryErrorCount(context.ActionDescriptor, context.Exception);
    }
}