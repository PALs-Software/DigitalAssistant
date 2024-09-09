using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Commands.Interfaces;

public interface ICommandResponse
{
    bool Success { get; }
    string? Response { get; }

    List<(IClient Client, IClientActionArgs Action)> ClientActions { get; }
    List<(IDevice Device, IDeviceActionArgs Action)> DeviceActions { get; }
}