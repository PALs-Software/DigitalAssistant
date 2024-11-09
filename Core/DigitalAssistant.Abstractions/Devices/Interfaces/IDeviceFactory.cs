using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Enums;

namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface IDeviceFactory
{
    ILightDevice CreateLightDevice(IConnector connector, string internalId, string name, DeviceStatus status,
        string manufacturer, string productName,
        bool on, bool isDimmable, double brightness,
        bool colorTemperatureIsAdjustable, int? colorTemperature, int minimumColorTemperature, int maximumColorTemperature,
        bool colorIsAdjustable, string? color,
        string? additionalJsonData = null);

    ISwitchDevice CreateSwitchDevice(IConnector connector, string internalId, string name, DeviceStatus status,
      string manufacturer, string productName, bool on,
      string? additionalJsonData = null);
}