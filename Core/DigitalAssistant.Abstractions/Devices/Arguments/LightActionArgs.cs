using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Devices.Arguments;

public class LightActionArgs : IDeviceActionArgs
{
    public bool? On { get; set; }
    public double? Brightness { get; set; }
    public double? BrightnessDelta { get; set; }
    public bool SetColorTemperature { get; set; }
    public int? ColorTemperature { get; set; }
    public ColorTemperatureColor? ColorTemperatureColor { get; set; }
    public int? ColorTemperatureDelta { get; set; }
    public string? Color { get; set; }
}
