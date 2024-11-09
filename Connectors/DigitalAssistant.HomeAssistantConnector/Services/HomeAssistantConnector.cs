using BlazorBase.Abstractions.General.Extensions;
using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.HomeAssistantConnector.ApiModels;
using DigitalAssistant.HomeAssistantConnector.Components;
using DigitalAssistant.HomeAssistantConnector.Models;
using DigitalAssistant.HomeAssistantConnector.Properties;
using HassClient.Models;
using HassClient.WS;
using HassClient.WS.Messages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DigitalAssistant.HomeAssistantConnector.Services;

[UnsupportedOSPlatform("browser")]
public class HomeAssistantConnector : IConnector
{
    #region Properties
    internal HaConnectorSettings? Settings { get; private set; }
    #endregion

    #region Injects

    protected readonly IDeviceFactory DeviceFactory;
    protected readonly IDeviceChangeArgsFactory DeviceChangeArgsFactory;
    protected readonly IDataProtectionService DataProtectionService;
    protected readonly IStringLocalizer Localizer;
    protected readonly ILogger Logger;

    protected readonly Func<IConnector, string, DeviceType, Task<IDevice?>> GetDeviceAsync;
    protected readonly Func<List<IDeviceChangeArgs>, Task> OnDeviceChangedAsync;
    #endregion

    #region Properties
    public string Name => Localizer["Name"];
    public string Description => Localizer["Description"];
    public string Base64JpgImage => Convert.ToBase64String(Resources.HomeAssistantLogo);
    public bool Enabled => Settings != null;

    public Type SetupComponentType => typeof(EnableConnectorSetup);
    #endregion

    #region Members
    protected HassWSApi? Client;
    protected SemaphoreSlim Semaphore = new(1, 1);
    #endregion

    #region Consts
    protected const string DISCOVER_HUE_BRIDGE_URL = "https://discovery.meethue.com";
    protected const string GENERATE_HUE_API_KEY_ENDPOINT = "/api";
    protected const string GET_DEVICES_ENDPOINT = "/clip/v2/resource/device";
    protected const string DEVICE_ENDPOINT = "/clip/v2/resource/light/{0}";
    protected const string EVENTSTREAM_ENDPOINT = "/eventstream/clip/v2";
    #endregion

    #region Init
    public HomeAssistantConnector(string? connectorSettingsJson,
        Func<IConnector, string, DeviceType, Task<IDevice?>> getDeviceAsync,
        Func<List<IDeviceChangeArgs>, Task> onDeviceChangedAsync,
        IDeviceFactory deviceFactory,
        IDeviceChangeArgsFactory deviceChangeArgsFactory,
        IDataProtectionService dataProtectionService,
        IStringLocalizer localizer,
        ILogger logger)
    {
        Settings = String.IsNullOrEmpty(connectorSettingsJson) ? null : JsonSerializer.Deserialize<HaConnectorSettings?>(connectorSettingsJson);
        DeviceFactory = deviceFactory;
        DeviceChangeArgsFactory = deviceChangeArgsFactory;
        DataProtectionService = dataProtectionService;
        Localizer = localizer;
        Logger = logger;
        GetDeviceAsync = getDeviceAsync;
        OnDeviceChangedAsync = onDeviceChangedAsync;

        if (Enabled)
            Task.Run(async () => await RegisterListersForDeviceUpdatesAsync());
    }
    #endregion

    public async Task<(bool Success, string? ErrorMessage, IConnectorSettings? Settings)> RegisterAsync(string url, SecureString accessToken, CancellationToken cancellationToken = default)
    {
        var client = new HassWSApi();
        try
        {
            var connectionParameters = ConnectionParameters.CreateFromInstanceBaseUrl(url, accessToken.ToInsecureString());
            await client.ConnectAsync(connectionParameters);
        }
        catch (Exception e)
        {
            return (false, e.Message, null);
        }
        finally
        {
            try
            {
                await client.CloseAsync();
            }
            catch (Exception) { }
        }

        var settings = new HaConnectorSettings
        {
            Url = url,
            AccessTokenEncrypted = DataProtectionService.Protect(accessToken.ToInsecureString())
        };
        Settings = settings;

        await RegisterListersForDeviceUpdatesAsync();

        return (true, null, Settings);
    }

    public async Task DisableConnectorAsync(CancellationToken cancellationToken = default)
    {
        Settings = null;
        if (Client != null)
        {
            try
            {
                await Client.CloseAsync();
            }
            catch (Exception) { }

            Client = null;
        }
    }

    public async Task<List<IDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync().ConfigureAwait(false);
        var entities = await client.GetEntitiesAsync().ConfigureAwait(false);
        var states = await client.GetStatesAsync().ConfigureAwait(false);
        var haDevices = await GetHaDevicesAsync(client).ConfigureAwait(false);

        var devices = new List<IDevice>();
        foreach (var lightEntity in entities.Where(entry => entry.EntityId.StartsWith("light.")))
        {
            var device = haDevices.FirstOrDefault(entry => entry.Id == lightEntity.DeviceId);
            var state = states.FirstOrDefault(entry => entry.EntityId == lightEntity.EntityId);
            if (device == null || state == null)
                continue;

            var infos = GetLightInformationsFromState(state);
            if (infos == null)
                continue;

            var lightDevice = DeviceFactory.CreateLightDevice(this, lightEntity.EntityId, device.NameByUser ?? device.Name ?? string.Empty, DeviceStatus.Online,
                device.Manufacturer ?? string.Empty, device.Model ?? string.Empty,
                infos.Value.IsOn, infos.Value.BrightnessIsAdjustable, infos.Value.Brightness ?? 100,
                infos.Value.ColorTemperatureIsAdjustable, infos.Value.CurrentColorTemperature, infos.Value.MinimumColorTemperature ?? 0, infos.Value.MaximumColorTemperature ?? 0,
                infos.Value.ColorIsAdjustable, infos.Value.HexColor, null
            );
            devices.Add(lightDevice);
        }

        foreach (var switchEntity in entities.Where(entry => entry.EntityId.StartsWith("switch.")))
        {
            var device = haDevices.FirstOrDefault(entry => entry.Id == switchEntity.DeviceId);
            var state = states.FirstOrDefault(entry => entry.EntityId == switchEntity.EntityId);
            if (device == null || state == null)
                continue;

            var isOn = state.State == "on";

            var switchDevice = DeviceFactory.CreateSwitchDevice(this, switchEntity.EntityId, device.NameByUser ?? device.Name ?? string.Empty, DeviceStatus.Online,
                device.Manufacturer ?? string.Empty, device.Model ?? string.Empty, isOn,
                null
            );
            devices.Add(switchDevice);
        }

        return devices;
    }

    protected JsonNode? GetJsonAttributeNode(string key, StateModel state)
    {
        if (!state.Attributes.TryGetValue(key, out var value))
            return null;

        var valueString = value.Value as string;
        if (valueString == null)
            return null;

        return JsonObject.Parse(valueString);
    }

    protected string ConvertRgbColorToHex(int r, int g, int b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    #region Device Actions

    public async Task<(bool Success, string? ErrorMessage)> ExecuteDeviceActionAsync(IDevice device, IDeviceActionArgs args, CancellationToken cancellationToken = default)
    {
        switch (device.Type)
        {
            case DeviceType.Light:
                if (device is not ILightDevice lightDevice)
                    throw new ArgumentException($"The device \"{device.Name}\" has the type \"{device.Type}\" but is not a type of \"{nameof(ILightDevice)}\"");
                if (args is not LightActionArgs lightActionArgs)
                    throw new ArgumentException($"The action argument type \"{args.GetType()}\" is not from the type \"{nameof(LightActionArgs)}\"");

                return await ExecuteLightActionAsync(lightDevice, lightActionArgs, cancellationToken).ConfigureAwait(false);
            case DeviceType.Switch:
                if (device is not ISwitchDevice switchDevice)
                    throw new ArgumentException($"The device \"{device.Name}\" has the type \"{device.Type}\" but is not a type of \"{nameof(ISwitchDevice)}\"");
                if (args is not SwitchActionArgs switchActionArgs)
                    throw new ArgumentException($"The action argument type \"{args.GetType()}\" is not from the type \"{nameof(SwitchActionArgs)}\"");

                return await ExecuteSwitchActionAsync(switchDevice, switchActionArgs, cancellationToken).ConfigureAwait(false);
            default:
                throw new NotImplementedException($"The device type \"{device.Type}\" is not implemented for the connector \"{Name}\"");
        }
    }

    protected async Task<(bool Success, string? ErrorMessage)> ExecuteLightActionAsync(ILightDevice device, LightActionArgs args, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();

        var request = new UpdateLightRequest()
        {
            EntityId = device.InternalId,
            Brightness = GetValidBrightnessValue(device, args.Brightness) ?? GetValidBrightnessDeltaValue(device, args.BrightnessDelta),
            ColorTemperature = GetValidColorTemperatureValue(device, args.ColorTemperature, args.ColorTemperatureColor) ?? GetValidColorTemperatureDeltaValue(device, args.ColorTemperatureDelta),
            Color = GetValidColorValue(device, args.Color)
        };

        KnownServices knownService = KnownServices.TurnOn;
        if (request.Brightness == 0 || args.On == false)
            knownService = KnownServices.TurnOff;

        try
        {
            var success = await client.CallServiceAsync(KnownDomains.Light, knownService, data: request).ConfigureAwait(false);
            if (!success)
                return (false, Localizer["Device update was not successfull."]);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }

        return (true, null);
    }

    protected async Task<(bool Success, string? ErrorMessage)> ExecuteSwitchActionAsync(ISwitchDevice device, SwitchActionArgs args, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync();

        var request = new UpdateRequest()
        {
            EntityId = device.InternalId,
        };

        KnownServices knownService = args.On ?? false ? KnownServices.TurnOn : KnownServices.TurnOff;

        try
        {
            var success = await client.CallServiceAsync(KnownDomains.Switch, knownService, data: request).ConfigureAwait(false);
            if (!success)
                return (false, Localizer["Device update was not successfull."]);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }

        return (true, null);
    }

    protected int? GetValidBrightnessValue(ILightDevice device, double? brightness)
    {
        if (brightness == null || !device.IsDimmable)
            return null;

        return (int)Math.Max(0, Math.Min(100, brightness.Value));
    }

    protected int? GetValidBrightnessDeltaValue(ILightDevice device, double? brightnessDelta)
    {
        if (brightnessDelta == null || !device.IsDimmable)
            return null;

        return (int)Math.Max(0, Math.Min(100, device.Brightness + brightnessDelta.Value));
    }

    protected int? GetValidColorTemperatureValue(ILightDevice device, int? colorTemperature, ColorTemperatureColor? colorTemperatureColor)
    {
        if ((colorTemperature == null && colorTemperatureColor == null) || !device.ColorTemperatureIsAdjustable)
            return null;

        int? validColorTemperature = null;
        if (colorTemperature != null)
            validColorTemperature = Math.Max(device.MinimumColorTemperature, Math.Min(device.MaximumColorTemperature, colorTemperature.Value));
        else
        {
            switch (colorTemperatureColor)
            {
                case ColorTemperatureColor.WarmWhite:
                    validColorTemperature = device.MaximumColorTemperature;
                    break;
                case ColorTemperatureColor.NeutralWhite:
                    validColorTemperature = device.MinimumColorTemperature + (device.MaximumColorTemperature - device.MinimumColorTemperature) / 2;
                    break;
                case ColorTemperatureColor.ColdWhite:
                    validColorTemperature = device.MinimumColorTemperature;
                    break;
            }
        }

        return validColorTemperature;
    }

    protected int? GetValidColorTemperatureDeltaValue(ILightDevice device, int? colorTemperatureDelta)
    {
        if (colorTemperatureDelta == null || !device.ColorTemperatureIsAdjustable)
            return null;

        return Math.Max(device.MinimumColorTemperature, Math.Min(device.MaximumColorTemperature, (device.ColorTemperature ?? device.MinimumColorTemperature) + colorTemperatureDelta.Value));
    }

    protected int[]? GetValidColorValue(ILightDevice device, string? color)
    {
        if (color == null || !device.ColorIsAdjustable)
            return null;

        var rgbColor = ColorTranslator.FromHtml(color);
        return [rgbColor.R, rgbColor.G, rgbColor.B];
    }

    #endregion

    public async Task RegisterListersForDeviceUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(Settings?.AccessTokenEncrypted))
            return;

        var client = await GetClientAsync().ConfigureAwait(false);
        client.StateChangedEventListener.SubscribeDomainStatusChanged("light", this.UpdateDeviceInformations);
        client.StateChangedEventListener.SubscribeDomainStatusChanged("switch", this.UpdateDeviceInformations);
    }

    protected void UpdateDeviceInformations(object? sender, StateChangedEvent changedEvent)
    {
        if (changedEvent.Domain != "light" && changedEvent.Domain != "switch")
            return;

        Task.Run(async () =>
        {
            var deviceChangeArgs = new List<IDeviceChangeArgs>();

            var deviceType = GetDeviceTypeByDomain(changedEvent.Domain);
            var dbDevice = await GetDeviceAsync(this, changedEvent.EntityId, deviceType);
            if (dbDevice == null)
                await AddNewDeviceAsync(deviceType, changedEvent, deviceChangeArgs);
            else
                await UpdateDeviceAsync(deviceType, changedEvent, deviceChangeArgs);

            if (deviceChangeArgs.Count > 0)
                await OnDeviceChangedAsync.Invoke(deviceChangeArgs);
        });
    }

    protected async Task AddNewDeviceAsync(DeviceType deviceType, StateChangedEvent changedEvent, List<IDeviceChangeArgs> deviceChangeArgs)
    {
        var client = await GetClientAsync();
        
        var entities = await client.GetEntitiesAsync().ConfigureAwait(false);
        var entity = entities?.FirstOrDefault(entry => entry.EntityId == changedEvent.EntityId);
        if (entity == null)
            return;

        var haDevices = await GetHaDevicesAsync(client).ConfigureAwait(false);
        var device = haDevices.FirstOrDefault(entry => entry.Id == entity.DeviceId);
        if (device == null)
            return;

        switch (deviceType)
        {
            case DeviceType.Light:
                var infos = GetLightInformationsFromState(changedEvent.NewState);
                if (infos == null)
                    return;

                var lightDevice = DeviceFactory.CreateLightDevice(this, changedEvent.EntityId, device.NameByUser ?? device.Name ?? string.Empty, DeviceStatus.Online,
                    device.Manufacturer ?? string.Empty, device.Model ?? string.Empty,
                    infos.Value.IsOn, infos.Value.BrightnessIsAdjustable, infos.Value.Brightness ?? 100,
                    infos.Value.ColorTemperatureIsAdjustable, infos.Value.CurrentColorTemperature, infos.Value.MinimumColorTemperature ?? 0, infos.Value.MaximumColorTemperature ?? 0,
                    infos.Value.ColorIsAdjustable, infos.Value.HexColor, null
                );

                deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateAddDeviceArgs(this, lightDevice));
                break;
            case DeviceType.Switch:
                var isOn = changedEvent.NewState.State == "on";

                var switchDevice = DeviceFactory.CreateSwitchDevice(this, changedEvent.EntityId, device.NameByUser ?? device.Name ?? string.Empty, DeviceStatus.Online,
                    device.Manufacturer ?? string.Empty, device.Model ?? string.Empty, isOn,
                    null
                );

                deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateAddDeviceArgs(this, switchDevice));
                break;
            default:
                break;
        }
    }

    protected async Task UpdateDeviceAsync(DeviceType deviceType, StateChangedEvent changedEvent, List<IDeviceChangeArgs> deviceChangeArgs)
    {
        var client = await GetClientAsync();

        switch (deviceType)
        {
            case DeviceType.Light:
                var infos = GetLightInformationsFromState(changedEvent.NewState);
                if (infos == null)
                    return;

                var lightActionArgs = new LightActionArgs()
                {
                    On = infos.Value.IsOn,
                    Brightness = infos.Value.Brightness,
                    SetColorTemperature = infos.Value.CurrentColorTemperature != null,
                    ColorTemperature = infos.Value.CurrentColorTemperature,
                    Color = infos.Value.HexColor
                };

                deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateUpdateDeviceArgs(this, changedEvent.EntityId, lightActionArgs));
                break;
            case DeviceType.Switch:
                var isOn = changedEvent.NewState.State == "on";

                var switchActionArgs = new SwitchActionArgs()
                {
                    On = isOn
                };

                deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateUpdateDeviceArgs(this, changedEvent.EntityId, switchActionArgs));
                break;
            default:
                break;
        }
    }

    protected DeviceType GetDeviceTypeByDomain(string domain)
    {
        return domain switch
        {
            "light" => DeviceType.Light,
            "switch" => DeviceType.Switch,
            _ => throw new NotImplementedException($"The domain \"{domain}\" is not implemented for the connector \"{Name}\""),
        };
    }

    #region MISC

    protected async Task<HassWSApi> GetClientAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            if (!Enabled || Settings == null)
                throw new Exception(Localizer["Connector is not enabled or no settings can be found. Please register the connector first."]);

            if (Client == null)
            {
                Client = new HassWSApi();
                var connectionParameters = ConnectionParameters.CreateFromInstanceBaseUrl(Settings.Url, DataProtectionService.Unprotect(Settings.AccessTokenEncrypted));
                await Client.ConnectAsync(connectionParameters).ConfigureAwait(false);
            }
        }
        finally
        {
            Semaphore.Release();
        }

        return Client;
    }

    protected (bool IsOn, bool BrightnessIsAdjustable, bool ColorTemperatureIsAdjustable, bool ColorIsAdjustable,
        int? Brightness, int? CurrentColorTemperature, int? MinimumColorTemperature,
        int? MaximumColorTemperature, string? HexColor)?
        GetLightInformationsFromState(StateModel state)
    {
        if (state == null)
            return null;

        var supportedColorModes = GetJsonAttributeNode("supported_color_modes", state)?.AsArray()?.GetValues<string>();

        var brightnessIsAdjustable = supportedColorModes != null && supportedColorModes.Contains("brightness");
        var colorTemperatureIsAdjustable = supportedColorModes != null && supportedColorModes.Contains("color_temp");
        var colorIsAdjustable = supportedColorModes != null && supportedColorModes.Contains("xy");

        var isOn = state.State == "on";

        int? brightness = GetJsonAttributeNode("brightness", state)?.GetValue<int?>();
        if (brightness != null)
        {
            brightnessIsAdjustable = true;
            brightness = (int)(brightness / 255f * 100f);
        }

        int? currentColorTemperature = null;
        int? minimumColorTemperature = null;
        int? maximumColorTemperature = null;
        if (colorTemperatureIsAdjustable)
        {
            currentColorTemperature = GetJsonAttributeNode("color_temp", state)?.GetValue<int?>();
            minimumColorTemperature = GetJsonAttributeNode("min_mireds", state)?.GetValue<int?>();
            maximumColorTemperature = GetJsonAttributeNode("max_mireds", state)?.GetValue<int?>();
        }

        string? hexColor = null;
        if (colorIsAdjustable)
        {
            var array = GetJsonAttributeNode("rgb_color", state)?.AsArray();
            if (array != null && array.Count == 3)
            {
                var r = array[0]?.GetValue<int?>();
                var g = array[1]?.GetValue<int?>();
                var b = array[2]?.GetValue<int?>();
                if (r != null && g != null && b != null)
                    hexColor = ConvertRgbColorToHex(r.Value, g.Value, b.Value);
            }
        }

        return (isOn, brightnessIsAdjustable, colorTemperatureIsAdjustable, colorIsAdjustable,
            brightness, currentColorTemperature, minimumColorTemperature,
            maximumColorTemperature, hexColor);
    }

    public async Task<List<HaDevice>> GetHaDevicesAsync(HassWSApi client)
    {
        var rawResult = await client.SendRawCommandWithResultAsync(new RawCommandMessage("config/device_registry/list")).ConfigureAwait(false);
        if (!rawResult.Success || rawResult.Result.Value == null)
            return [];

        return JsonSerializer.Deserialize<List<HaDevice>>((string)rawResult.Result.Value) ?? [];
    }


    #endregion
}
