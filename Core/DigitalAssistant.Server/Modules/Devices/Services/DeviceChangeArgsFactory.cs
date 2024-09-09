using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Server.Modules.Devices.Arguments;
using DigitalAssistant.Server.Modules.Devices.Models;

namespace DigitalAssistant.Server.Modules.Devices.Services;

public class DeviceChangeArgsFactory : IDeviceChangeArgsFactory
{
    public IDeviceChangeArgs CreateAddDeviceArgs(IConnector connector, IDevice device)
    {
        return new DeviceChangeArgs()
        {
            Type = DeviceChangeType.Add,
            Connector = connector,
            Device = device
        };
    }

    public IDeviceChangeArgs CreateUpdateDeviceArgs(IConnector connector, string internalId, IDeviceActionArgs args)
    {
        return new DeviceChangeArgs()
        {
            Type = DeviceChangeType.Update,
            Connector = connector,
            Device = new Device()
            {
                InternalId = internalId
            },
            ActionArgs = args
        };
    }

    public IDeviceChangeArgs CreateRenameDeviceArgs(IConnector connector, string internalId, string newName)
    {
        return new DeviceChangeArgs()
        {
            Type = DeviceChangeType.Rename,
            Connector = connector,
            Device = new Device()
            {
                InternalId = internalId,
                Name = newName
            }
        };
    }

    public IDeviceChangeArgs CreateDeleteDeviceArgs(IConnector connector, string internalId)
    {
        return new DeviceChangeArgs()
        {
            Type = DeviceChangeType.Delete,
            Connector = connector,
            Device = new Device()
            {
                InternalId = internalId
            }
        };
    }
}
