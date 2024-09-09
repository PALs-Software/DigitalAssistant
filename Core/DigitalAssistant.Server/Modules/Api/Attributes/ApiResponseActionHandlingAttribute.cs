using DigitalAssistant.Server.Modules.Api.ApiModels;
using DigitalAssistant.Server.Modules.Api.Enums;
using DigitalAssistant.Server.Modules.CacheModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DigitalAssistant.Server.Modules.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiResponseActionHandlingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Cache.TelemetryCache.IncreaseApiTelemetryCount(context.ActionDescriptor);

        if (context.ModelState.IsValid || context.Controller is not ApiController apiController)
            return;

        context.Result = new ObjectResult(CreateModelStateInvalidErrorResponse(apiController, context.ModelState));
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {        
        if (context.Exception == null || context.Controller is not ApiController apiController)
            return;

        context.ExceptionHandled = true;

        if (context.Exception is ApiException apiException)
            context.Result = new ObjectResult(apiController.CreateErrorResponse(apiException));
        else
            context.Result = new ObjectResult(apiController.CreateErrorResponse(ApiErrorCode.UndefinedError, context.Exception));
    }

    internal ApiResponse CreateModelStateInvalidErrorResponse(ApiController controller, ModelStateDictionary modelState)
    {
        List<string> errorMessages = new List<string>();
        foreach (var modelStateKey in modelState.Keys)
            foreach (ModelError error in modelState[modelStateKey]!.Errors)
                errorMessages.Add($"• {error.ErrorMessage}");

        return controller.CreateErrorResponse(ApiErrorCode.ModelIsInvalid, String.Join(Environment.NewLine, errorMessages));
    }
}