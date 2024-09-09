using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class GenerateHueApiKeyResponse
{
    [JsonPropertyName("error")]
    public HueApiError? Error { get; set; }

    [JsonPropertyName("success")]
    public GenerateHueApiKeySuccess? Success { get; set; }
}

public class GenerateHueApiKeySuccess
{
    [JsonPropertyName("username")]
    public string? UserName { get; set; }

    [JsonPropertyName("clientkey")]
    public string? ClientKey { get; set; }
}