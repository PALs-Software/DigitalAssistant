using DigitalAssistant.Abstractions.Connectors;

namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface IDeviceChangeArgsFactory
{
    IDeviceChangeArgs CreateAddDeviceArgs(IConnector connector, IDevice device);
    IDeviceChangeArgs CreateUpdateDeviceArgs(IConnector connector, string internalId, IDeviceActionArgs args);
    IDeviceChangeArgs CreateRenameDeviceArgs(IConnector connector, string internalId, string newName);
    IDeviceChangeArgs CreateDeleteDeviceArgs(IConnector connector, string internalId);
}