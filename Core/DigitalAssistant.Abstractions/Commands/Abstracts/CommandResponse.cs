using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Commands.Interfaces;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Commands.Abstracts;

public class CommandResponse : ICommandResponse
{
    public CommandResponse(bool success, string? response = null)
    {
        Success = success;
        Response = response;
        ClientActions = [];
        DeviceActions = [];
    }

    public CommandResponse(bool success, string? response, List<(IDevice Device, IDeviceActionArgs Action)> deviceActions)
    {
        Success = success;
        Response = response;
        ClientActions = [];
        DeviceActions = deviceActions;
    }

    public CommandResponse(bool success, string? response, List<(IClient Device, IClientActionArgs Action)> clientActions)
    {
        Success = success;
        Response = response;
        ClientActions = clientActions;
        DeviceActions = [];
    }

    public bool Success { get; init; }
    public string? Response { get; init; }
    public List<(IClient Client, IClientActionArgs Action)> ClientActions { get; init; }
    public List<(IDevice Device, IDeviceActionArgs Action)> DeviceActions { get; init; }

}
