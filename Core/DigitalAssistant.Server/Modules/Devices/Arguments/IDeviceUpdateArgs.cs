using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Server.Modules.Devices.Arguments;

public class DeviceChangeArgs: IDeviceChangeArgs
{
    public DeviceChangeType Type { get; set; }
    public IConnector Connector { get; set; } = null!;
    public IDevice Device { get; set; } = null!;
    public IDeviceActionArgs? ActionArgs { get; set; }
}
