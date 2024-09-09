namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface ILightDevice : IDevice
{
    bool On { get; set; }
    bool IsDimmable { get; set; }
    double Brightness { get; set; }
    bool ColorTemperatureIsAdjustable { get; set; }
    int? ColorTemperature { get; set; }
    int MinimumColorTemperature { get; set; }
    int MaximumColorTemperature { get; set; }
    bool ColorIsAdjustable { get; set; }
    string? Color { get; set; }
}
