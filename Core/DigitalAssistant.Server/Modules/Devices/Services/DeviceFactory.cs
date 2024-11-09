using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Server.Modules.Devices.Models;

namespace DigitalAssistant.Server.Modules.Devices.Services;

public class DeviceFactory : IDeviceFactory
{
    public ILightDevice CreateLightDevice(IConnector connector, string internalId, string name, DeviceStatus status,
        string manufacturer, string productName,
        bool on, bool isDimmable, double brightness,
        bool colorTemperatureIsAdjustable, int? colorTemperature, int minimumColorTemperature, int maximumColorTemperature,
        bool colorIsAdjustable, string? color,
        string? additionalJsonData = null)
    {
        return new LightDevice()
        {
            InternalId = internalId,
            Name = name,
            Type = DeviceType.Light,
            Status = status,
            Connector = connector.Name,
            Manufacturer = manufacturer,
            ProductName = productName,
            On = on,
            IsDimmable = isDimmable,
            Brightness = brightness,
            ColorTemperatureIsAdjustable = colorTemperatureIsAdjustable,
            ColorTemperature = colorTemperature,
            MinimumColorTemperature = minimumColorTemperature,
            MaximumColorTemperature = maximumColorTemperature,
            ColorIsAdjustable = colorIsAdjustable,
            Color = color,
            AdditionalJsonData = additionalJsonData
        };
    }

    public ISwitchDevice CreateSwitchDevice(IConnector connector, string internalId, string name, DeviceStatus status,
        string manufacturer, string productName, bool on,
        string? additionalJsonData = null)
    {
        return new SwitchDevice()
        {
            InternalId = internalId,
            Name = name,
            Type = DeviceType.Switch,
            Status = status,
            Connector = connector.Name,
            Manufacturer = manufacturer,
            ProductName = productName,
            On = on,          
            AdditionalJsonData = additionalJsonData
        };
    }
}
