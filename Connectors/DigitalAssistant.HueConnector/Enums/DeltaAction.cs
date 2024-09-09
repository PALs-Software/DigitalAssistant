using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeltaAction
{
    up,
    down,
    stop
}
