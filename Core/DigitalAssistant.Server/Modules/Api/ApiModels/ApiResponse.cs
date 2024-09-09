namespace DigitalAssistant.Server.Modules.Api.ApiModels;

public class ApiResponse
{
    public ApiResponse() { }
    public ApiResponse(bool success)
    {
        Success = success;
    }

    public ApiResponse(object responseData)
    {
        Success = true;
        ResponseData = responseData;
    }
    public ApiResponse(string error)
    {
        Error = error;
    }

    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? ResponseData { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public T? ResponseData { get; set; }
}