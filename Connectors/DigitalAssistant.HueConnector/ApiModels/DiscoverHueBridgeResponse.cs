using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class DiscoverHueBridgeResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("internalipaddress")]
    public string? InternalIpAddress { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }
}
