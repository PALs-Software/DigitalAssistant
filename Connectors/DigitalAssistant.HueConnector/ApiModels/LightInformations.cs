using DigitalAssistant.HueConnector.Enums;
using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.ApiModels;

public class HueApiError
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class On
{
    [JsonPropertyName("on")]
    public bool IsOn { get; set; }
}

public class Dimming
{
    [JsonPropertyName("brightness")]
    public double Brightness { get; set; } = 100;
}

public class DimmingDelta
{
    [JsonPropertyName("action")]
    public DeltaAction Action { get; set; }

    [JsonPropertyName("brightness_delta")]
    public double BrightnessDelta { get; set; }
}

public class ColorTemperature
{
    [JsonPropertyName("mirek")]
    public int? Mirek { get; set; }

    [JsonPropertyName("mirek_schema")]
    public MirekSchema? Schema { get; set; }
}

public class ColorTemperatureDelta
{
    [JsonPropertyName("action")]
    public DeltaAction Action { get; set; }

    [JsonPropertyName("mirek_delta")]
    public int MirekDelta { get; set; }
}

public class MirekSchema
{
    [JsonPropertyName("mirek_minimum")]
    public int Minimum { get; set; }

    [JsonPropertyName("mirek_maximum")]
    public int Maximum { get; set; }
}

public class HueColor
{
    [JsonPropertyName("xy")]
    public XyPoint Xy { get; set; } = new();

    [JsonPropertyName("gamut")]
    public Gamut? Gamut { get; set; }
}

public class XyPoint
{
    public XyPoint() { }
    public XyPoint(double x, double y) { X = x; Y = y; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public class Gamut
{
    [JsonPropertyName("red")]
    public XyPoint Red { get; set; } = new(1.0f, 0.0f);

    [JsonPropertyName("green")]
    public XyPoint Green { get; set; } = new(0.0f, 1.0f);

    [JsonPropertyName("blue")]
    public XyPoint Blue { get; set; } = new(0.0f, 0.0f);
}