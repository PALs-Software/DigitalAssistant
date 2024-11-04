using DigitalAssistant.Abstractions.Devices.Enums;

namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface IDevice
{
    string InternalId { get; set; }

    string Name { get; set; }

    List<string> AlternativeNames { get; set; }

    bool CustomName { get; set; }

    DeviceType Type { get; set; }

    DeviceStatus Status { get; set; }

    string Connector { get; set; }

    string Manufacturer { get; set; }

    string ProductName { get; set; }

    string? AdditionalJsonData { get; set; }
}
