using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class GenerateHueApiKeyRequest
{
    [JsonPropertyName("devicetype")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("generateclientkey")]
    public bool GenerateClientKey { get; set; } = true;
}