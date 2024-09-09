using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class UpdateLightResponse
{
    [JsonPropertyName("data")]
    public List<UpdateLightResponseData>? Data { get; set; }

    [JsonPropertyName("error")]
    public List<HueApiError>? Errors { get; set; }
}

public class UpdateLightResponseData
{
    [JsonPropertyName("rid")]
    public string? Id { get; set; }

    [JsonPropertyName("rtype")]
    public string? Type { get; set; }
}