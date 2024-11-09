using Newtonsoft.Json;

namespace DigitalAssistant.HomeAssistantConnector.ApiModels;

public class UpdateRequest
{
    [JsonProperty(PropertyName = "entity_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? EntityId { get; set; }
}