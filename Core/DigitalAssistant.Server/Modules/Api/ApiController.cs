using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.Abstractions.General.Extensions;
using BlazorBase.AudioRecorder.Services;
using BlazorBase.Services;
using DigitalAssistant.Server.Modules.Api.ApiModels;
using DigitalAssistant.Server.Modules.Api.Attributes;
using DigitalAssistant.Server.Modules.Api.Enums;
using DigitalAssistant.Server.Modules.Api.Models;
using DigitalAssistant.Server.Modules.CacheModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Net.Mime;

namespace DigitalAssistant.Server.Modules.Api;

[ApiController]
[Route("[controller]")]
[ApiResponseActionHandling]
[TypeFilter(typeof(AuthorizationFilter))]
[Produces(MediaTypeNames.Application.Json)]
public class ApiController(IServiceProvider serviceProvider, IBaseDbContext dbContext, IStringLocalizer<ApiController> localizer, BaseErrorHandler errorHandler, AudioConverter audioConverter) : ControllerBase
{
    #region Injects

    protected IServiceProvider ServiceProvider { get; set; } = serviceProvider;
    protected IBaseDbContext DbContext { get; set; } = dbContext;
    protected IStringLocalizer<ApiController> Localizer { get; set; } = localizer;
    protected BaseErrorHandler ErrorHandler { get; set; } = errorHandler;
    protected AudioConverter AudioConverter { get; set; } = audioConverter;
    #endregion

    #region Status

    [HttpGet]
    [AllowAnonymous]
    [Route("status/is-available")]
    public bool ServiceIsAvailable()
    {
        return true;
    }

    [HttpGet]
    [Route("status/api-token")]
    public ApiResponse TestApiTokenIsValid()
    {
        return CreateSuccessResponse(true);
    }

    #endregion

    #region Audio


    private static List<short> OverallSamples = [];

    [HttpPost]
    [AllowAnonymous]
    [Route("audio/process")]
    [Consumes("multipart/form-data")]
    public async Task<ApiResponse> ProcessAudioAsync()
    {
        using MemoryStream memoryStream = new();
        await Request.Body.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();
        short[] samples = new short[(int)Math.Ceiling(bytes.Length / 2.0)];        
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);

        OverallSamples.AddRange(samples);
        var wavBytes = AudioConverter.ConvertSamplesToWav(OverallSamples.ToArray());
        System.IO.File.WriteAllBytes(@"C:\\Temp\test.wav", wavBytes);

        return CreateSuccessResponse(true);
    }
    #endregion

    #region Api Responses

    public ApiResponse CreateModelIsInvalidErrorResponse(string untranslatedErrorMessage, params object[] arguments)
    {
        return CreateErrorResponse(ApiErrorCode.ModelIsInvalid, untranslatedErrorMessage, arguments);
    }

    public ApiResponse CreateErrorResponse(ApiErrorCode apiErrorCode, string untranslatedErrorMessage, params object[] arguments)
    {
        var errorMessage = Localizer["An error has appeared in the webservice: {0}", Localizer[apiErrorCode.ToString(), Localizer[untranslatedErrorMessage, arguments]]];
        Cache.TelemetryCache.IncreaseApiTelemetryErrorCount(this.Url.ActionContext.ActionDescriptor, new Exception(errorMessage));

        return new ApiResponse()
        {
            Error = errorMessage
        };
    }

    public ApiResponse CreateErrorResponse(ApiErrorCode apiErrorCode, Exception e)
    {
        return CreateErrorResponse(apiErrorCode, ErrorHandler.PrepareExceptionErrorMessage(e));
    }

    public ApiResponse CreateErrorResponse(ApiException e)
    {
        return CreateErrorResponse(e.ApiErrorCode, e.Message, e.Arguments);
    }

    protected ApiResponse CreateSuccessResponse(object? responseData = null)
    {
        return new ApiResponse()
        {
            Success = true,
            ResponseData = responseData
        };
    }

    #endregion

    #region Token

    public AccessToken GetCurrentAccessToken()
    {
        var token = HttpContext.Request.Headers.Authorization.ToString();
        if (String.IsNullOrEmpty(token))
        {
            var exception = new Exception("No access token was transmitted");
            Cache.TelemetryCache.IncreaseApiTelemetryErrorCount("authorization/accessTokenNotExistsInHeaderByGetCurrentAccessToken", exception);
            throw exception;
        }

        var hash = token.CreateSHA512Hash();
        if (!Cache.ApiCache.AccessTokens.TryGetValue(hash, out AccessToken? value))
        {
            var exception = new Exception("The transmitted access token is not valid");
            Cache.TelemetryCache.IncreaseApiTelemetryErrorCount("authorization/accessTokenNotValidByGetCurrentAccessToken", exception);
            throw exception;
        }

        return value;
    }

    #endregion
}
