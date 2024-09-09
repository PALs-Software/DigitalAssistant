using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Enums;

namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface IDeviceChangeArgs
{
    DeviceChangeType Type { get; set; }
    IConnector Connector { get; set; }
    IDevice Device { get; set; }
    IDeviceActionArgs? ActionArgs { get; set; }
}
