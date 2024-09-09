using BlazorBase.Abstractions.CRUD.Extensions;
using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Connectors.Models;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Plugins;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace DigitalAssistant.Server.Modules.Connectors.Services;

public class ConnectorService(IServiceProvider serviceProvider, IDataProtectionService dataProtectionService, ILogger<ConnectorService> logger, IStringLocalizerFactory stringLocalizerFactory, ILoggerFactory loggerFactory,
    IDeviceFactory deviceFactory, IDeviceChangeArgsFactory deviceChangeArgsFactory)
{
    #region Injects
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly IDataProtectionService DataProtectionService = dataProtectionService;
    protected readonly ILogger<ConnectorService> Logger = logger;
    protected readonly IStringLocalizerFactory StringLocalizerFactory = stringLocalizerFactory;
    protected readonly ILoggerFactory LoggerFactory = loggerFactory;
    protected readonly IDeviceFactory DeviceFactory = deviceFactory;
    protected readonly IDeviceChangeArgsFactory DeviceChangeArgsFactory = deviceChangeArgsFactory;
    #endregion

    #region Members
    protected readonly Dictionary<string, IConnector> Connectors = [];
    #endregion

    public IReadOnlyList<IConnector> GetConnectors()
    {
        return Connectors.Values.ToList().AsReadOnly();
    }

    #region Load Connectors

    public async Task LoadConnectorsAsync()
    {
        var connectorFolderPath = Path.Combine(AppContext.BaseDirectory, "Connectors");
        var connectorPathes = Directory.GetFiles(connectorFolderPath, "*Connector.dll", SearchOption.AllDirectories);
        foreach (var connectorPath in connectorPathes)
        {
            var connectorAssembly = LoadConnector(connectorPath);
            foreach (var connector in await CreateConnectorsAsync(connectorAssembly).ConfigureAwait(false))
                Connectors.Add(connector.Name, connector);
        }
    }

    protected Assembly LoadConnector(string path)
    {
        var assemblyName = AssemblyName.GetAssemblyName(path);
        Logger.LogInformation("Loading connector '{AssemblyName}'", assemblyName);

        var loadContext = new PluginLoadContext(path);
        return loadContext.LoadFromAssemblyName(assemblyName);
    }

    protected async Task<List<IConnector>> CreateConnectorsAsync(Assembly assembly)
    {
        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var connectors = new List<IConnector>();
        var connectorTypes = assembly.GetTypes().Where(type => typeof(IConnector).IsAssignableFrom(type));
        foreach (var connectorType in connectorTypes)
        {
            var localizer = StringLocalizerFactory.Create(connectorType);
            var logger = LoggerFactory.CreateLogger(connectorType);

            var connectorTypeName = connectorType.AssemblyQualifiedName;
            var connectorSettings = (await dbContext.WhereAsync<ConnectorSettings>(entry => entry.Type == connectorTypeName).ConfigureAwait(false)).FirstOrDefault();
            var connector = Activator.CreateInstance(connectorType, connectorSettings?.SettingsAsJson,
                (Func<IConnector, string, DeviceType, Task<IDevice?>>)GetDeviceAsync, (Func<List<IDeviceChangeArgs>, Task>)OnDeviceChangedAsync,
                DeviceFactory, DeviceChangeArgsFactory, DataProtectionService, localizer, logger) as IConnector;

            if (connector != null)
                connectors.Add(connector);
        }

        return connectors;
    }

    #endregion


    #region Discover and Update Devices

    public async Task DiscoverAndUpdateDevicesAsync()
    {
        foreach (var connector in Connectors.Values)
        {
            if (!connector.Enabled)
                continue;

            await DiscoverAndUpdateDevicesAsync(connector).ConfigureAwait(false);
        }
    }

    public async Task DiscoverAndUpdateDevicesAsync(IConnector connector)
    {
        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var commandHandler = ServiceProvider.GetRequiredService<CommandHandler>();

        var devices = await connector.GetDevicesAsync().ConfigureAwait(false);
        var dbConnectorDevices = await dbContext.WhereAsync<Device>(entry => entry.Connector == connector.Name).ConfigureAwait(false);

        var deviceInteralIds = devices.Select(entry => entry.InternalId).ToList();
        var notAvailableDevices = dbConnectorDevices.Where(entry => !deviceInteralIds.Contains(entry.InternalId));
        foreach (var notAvailableDevice in notAvailableDevices)
            notAvailableDevice.Status = DeviceStatus.Offline;

        foreach (var device in devices)
        {
            var deviceDbEntry = dbConnectorDevices.FirstOrDefault(entry => entry.Type == device.Type && entry.InternalId == device.InternalId);
            if (deviceDbEntry == null)
                await dbContext.AddAsync(device).ConfigureAwait(false);
            else
            {
                device.TransferPropertiesTo(deviceDbEntry,
                    nameof(Device.Id),
                    nameof(Device.Type),
                    nameof(Device.Name),
                    nameof(Device.AlternativeNames),
                    nameof(Device.CustomName),
                    nameof(Device.Connector),
                    nameof(Device.CreatedOn),
                    nameof(Device.ModifiedOn));

                if (!deviceDbEntry.CustomName)
                    deviceDbEntry.Name = device.Name;
            }
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        await commandHandler.RefreshLocalizedCommandTemplatesCacheAsync(clearAllLanguages: true).ConfigureAwait(false);
    }

    protected async Task<IDevice?> GetDeviceAsync(IConnector connector, string internalId, DeviceType deviceType)
    {
        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        return await dbContext.FirstOrDefaultAsync<Device>(entry => entry.Connector == connector.Name &&
                                                           entry.Type == deviceType &&
                                                           entry.InternalId == internalId,
                                                           asNoTracking: true);
    }

    protected async Task OnDeviceChangedAsync(List<IDeviceChangeArgs> deviceChangeArgs)
    {
        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var commandHandler = ServiceProvider.GetRequiredService<CommandHandler>();

        foreach (var args in deviceChangeArgs)
        {
            var device = await dbContext.FirstOrDefaultAsync<Device>(entry => entry.Connector == args.Connector.Name && entry.InternalId == args.Device.InternalId).ConfigureAwait(false);
            switch (args.Type)
            {
                case DeviceChangeType.Add:
                    if (device != null)
                        continue;

                    await dbContext.AddAsync(args.Device);
                    break;

                case DeviceChangeType.Update:
                    if (device == null || args.ActionArgs == null)
                        continue;

                    switch (device.Type)
                    {
                        case DeviceType.Light:
                            if (device is not ILightDevice lightDevice || args.ActionArgs is not LightActionArgs lightActionArgs)
                                continue;

                            lightDevice.On = lightActionArgs.On ?? lightDevice.On;
                            lightDevice.Brightness = lightActionArgs.Brightness ?? lightDevice.Brightness;
                            lightDevice.Brightness = lightActionArgs.BrightnessDelta == null ? lightDevice.Brightness : lightDevice.Brightness + lightActionArgs.BrightnessDelta.Value;
                            lightDevice.ColorTemperature = lightActionArgs.SetColorTemperature ? lightActionArgs.ColorTemperature : lightDevice.ColorTemperature;
                            lightDevice.ColorTemperature = lightActionArgs.ColorTemperatureDelta == null ? lightDevice.ColorTemperature : lightDevice.ColorTemperature + lightActionArgs.ColorTemperatureDelta.Value;
                            lightDevice.Color = lightActionArgs.Color ?? lightDevice.Color;
                            break;
                        case DeviceType.Switch:
                            break;
                    }
                    break;

                case DeviceChangeType.Rename:
                    if (device == null)
                        continue;

                    device.Name = args.Device.Name;
                    break;

                case DeviceChangeType.Delete:
                    if (device == null)
                        continue;

                    await dbContext.RemoveAsync(device);
                    break;
            }
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        if (deviceChangeArgs.Any(entry => entry.Type == DeviceChangeType.Add || entry.Type == DeviceChangeType.Delete || entry.Type == DeviceChangeType.Rename))
            await commandHandler.RefreshLocalizedCommandTemplatesCacheAsync(clearAllLanguages: true).ConfigureAwait(false);
    }

    #endregion

    #region Update Actions

    public Task<(bool Success, string? ErrorMessage)> ExecuteDeviceActionAsync(IDevice device, IDeviceActionArgs args)
    {
        if (!Connectors.TryGetValue(device.Connector, out var connector))
            throw new Exception($"Connector '{device.Connector}' of device {device.Name} not found");

        return connector.ExecuteDeviceActionAsync(device, args);
    }

    #endregion

}
