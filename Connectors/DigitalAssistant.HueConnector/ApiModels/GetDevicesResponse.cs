using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class GetDevicesResponse
{
    [JsonPropertyName("data")]
    public List<DeviceResponse> Data { get; set; } = [];
}

public class DeviceResponse
{
    [Required]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("product_data")]
    public ProductDataResponse? ProductData { get; set; }

    [JsonPropertyName("metadata")]
    public MetaDataResponse? MetaData { get; set; }

    [JsonPropertyName("services")]
    public List<ServicesResponse> Services { get; set; } = [];
}

public class MetaDataResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class ProductDataResponse
{
    [JsonPropertyName("manufacturer_name")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }
}

public class ServicesResponse
{
    [JsonPropertyName("rid")]
    public string? Id { get; set; }

    [JsonPropertyName("rtype")]
    public string? Type { get; set; }
}