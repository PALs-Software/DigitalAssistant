using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class EventStreamResponse
{
    [JsonPropertyName("creationtime")]
    public DateTimeOffset CreationTime { get; set; }

    [JsonPropertyName("data")]
    public List<EventStreamResponseData> Data { get; set; } = new();

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class EventStreamResponseData : DeviceResponse
{
    [JsonPropertyName("on")]
    public On? On { get; set; }

    [JsonPropertyName("dimming")]
    public Dimming? Dimming { get; set; }

    [JsonPropertyName("dimming_delta")]
    public DimmingDelta? DimmingDelta { get; set; }

    [JsonPropertyName("color_temperature")]
    public ColorTemperature? ColorTemperature { get; set; }

    [JsonPropertyName("color_temperature_delta")]
    public ColorTemperatureDelta? ColorTemperatureDelta { get; set; }

    [JsonPropertyName("color")]
    public HueColor? Color { get; set; }
}
