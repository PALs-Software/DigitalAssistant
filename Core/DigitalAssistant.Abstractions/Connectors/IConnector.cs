using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Connectors;

public interface IConnector
{
    string Name { get; }
    string Description { get; }
    string Base64JpgImage { get; }
    bool Enabled { get; }

    Type SetupComponentType { get; }

    Task<List<IDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task DisableConnectorAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage)> ExecuteDeviceActionAsync(IDevice device, IDeviceActionArgs args, CancellationToken cancellationToken = default);
}