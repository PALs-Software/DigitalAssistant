using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class GetDetailedDeviceDataResponse
{
    [JsonPropertyName("data")]
    public List<DetailedDeviceResponse> Data { get; set; } = [];
}

public class DetailedDeviceResponse
{
    [Required]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("on")]
    public On? On { get; set; }

    [JsonPropertyName("dimming")]
    public Dimming? Dimming { get; set; }

    [JsonPropertyName("color_temperature")]
    public ColorTemperature? ColorTemperature { get; set; }
    
    [JsonPropertyName("color")]
    public HueColor? Color { get; set; }
}