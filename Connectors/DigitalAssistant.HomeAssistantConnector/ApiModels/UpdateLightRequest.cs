using Newtonsoft.Json;

namespace DigitalAssistant.HomeAssistantConnector.ApiModels;

public class UpdateLightRequest : UpdateRequest
{
    [JsonProperty(PropertyName = "brightness_pct", NullValueHandling = NullValueHandling.Ignore)]
    public int? Brightness { get; set; }

    [JsonProperty(PropertyName = "color_temp", NullValueHandling = NullValueHandling.Ignore)]
    public int? ColorTemperature { get; set; }

    [JsonProperty(PropertyName = "rgb_color", NullValueHandling = NullValueHandling.Ignore)]
    public int[]? Color { get; set; }
}

