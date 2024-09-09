using DigitalAssistant.Server.Modules.Api.Enums;

namespace DigitalAssistant.Server.Modules.Api.ApiModels;
public class ApiException : Exception
{
    public ApiException(ApiErrorCode apiErrorCode, string untranslatedErrorMessage, params object[] arguments) : base(untranslatedErrorMessage)
    {
        ApiErrorCode = apiErrorCode;
        Arguments = arguments;
    }
    public ApiErrorCode ApiErrorCode { get; set; }
    public object[] Arguments { get; set; }
}
