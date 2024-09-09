using BlazorBase.Abstractions.General.Extensions;
using DigitalAssistant.Server.Modules.Api.Services;
using DigitalAssistant.Server.Modules.CacheModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DigitalAssistant.Server.Modules.Api.Attributes;

public class AuthorizationFilter(BruteForceDelayServiceFactory bruteForceDelayServiceFactory) : IAsyncAuthorizationFilter
{
    #region Injects
    protected readonly BruteForceDelayServiceFactory BruteForceDelayServiceFactory = bruteForceDelayServiceFactory;
    #endregion

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor == null)
        {
            context.Result = await GetAuthorizationWentWrongResultAsync("authorization/unknown", "Something went wrong", context);
            return;
        }

        if (descriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).FirstOrDefault() != null)
            return;

        var token = context.HttpContext.Request.Headers["Authorization"].ToString();
        if (String.IsNullOrEmpty(token))
        {
            context.Result = await GetAuthorizationWentWrongResultAsync("authorization/accessTokenNotTransmitted", "No access token was transmitted", context);
            return;
        }

        var hash = token.CreateSHA512Hash();
        if (!Cache.ApiCache.AccessTokens.ContainsKey(hash))
        {
            context.Result = await GetAuthorizationWentWrongResultAsync("authorization/accessTokenNotValid", "The transmitted access token is not valid", context);
            return;
        }

        if (Cache.ApiCache.AccessTokens[hash].ValidUntil < DateTime.Now)
        {
            context.Result = await GetAuthorizationWentWrongResultAsync("authorization/accessTokenNotValidAnymore", "The transmitted access token is not valid anymore", context);
            return;
        }

        var accessType = descriptor.MethodInfo.GetCustomAttributes(typeof(AccessTypeAttribute), false).FirstOrDefault() as AccessTypeAttribute;
        if (accessType == null)
            return;

        if (!accessType.Types.Contains(Cache.ApiCache.AccessTokens[hash].Type))
            context.Result = await GetAuthorizationWentWrongResultAsync("authorization/accessTokenNoAccessToThisEndpoint", "The transmitted access token has no access to this endpoint", context);
    }

    protected async Task<UnauthorizedObjectResult> GetAuthorizationWentWrongResultAsync(string endpoint, string message, AuthorizationFilterContext context)
    {
        Cache.TelemetryCache.IncreaseApiTelemetryCount(endpoint);
        Cache.TelemetryCache.IncreaseApiTelemetryErrorCount(endpoint, new Exception(message));

        await BruteForceDelayServiceFactory.GetOrCreate(context.HttpContext.Connection.RemoteIpAddress).DelayRequestAsync();

        return new UnauthorizedObjectResult(message);
    }
}