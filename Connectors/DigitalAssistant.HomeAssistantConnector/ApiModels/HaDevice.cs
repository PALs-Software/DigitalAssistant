using HassClient.Models;
using System.Text.Json.Serialization;

namespace DigitalAssistant.HomeAssistantConnector.ApiModels;

public class HaDevice
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("name_by_user")]
    public string? NameByUser { get; set; }

    [JsonPropertyName("area_id")]
    public string? AreaId { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("model_id")]
    public string? ModelId { get; set; }

}
