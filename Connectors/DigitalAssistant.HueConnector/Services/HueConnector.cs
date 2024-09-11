using BlazorBase.Abstractions.General.Extensions;
using DigitalAssistant.Abstractions.Connectors;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.HueConnector.ApiModels;
using DigitalAssistant.HueConnector.Components;
using DigitalAssistant.HueConnector.Enums;
using DigitalAssistant.HueConnector.Models;
using DigitalAssistant.HueConnector.Properties;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalAssistant.HueConnector.Services;

public class HueConnector : IConnector
{
    #region Properties
    internal HueConnectorSettings? Settings { get; private set; }
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
    public string Name => Localizer["Hue Connector"];
    public string Description => Localizer["Connects the Philips Hue Bridge to the Digital Assistant Server, allowing the Assistant to view and control devices connected to the Hue Bridge."];
    public string Base64JpgImage => Convert.ToBase64String(Resources.HueLogo);
    public bool Enabled => Settings != null;

    public Type SetupComponentType => typeof(EnableConnectorSetup);
    #endregion

    #region Members
    protected HttpClient? Client;
    protected Task? ListenForDeviceUpdatesTask;
    protected CancellationTokenSource? ListenForDeviceUpdatesTaskTokenSource;
    #endregion

    #region Consts
    protected const string DISCOVER_HUE_BRIDGE_URL = "https://discovery.meethue.com";
    protected const string GENERATE_HUE_API_KEY_ENDPOINT = "/api";
    protected const string GET_DEVICES_ENDPOINT = "/clip/v2/resource/device";
    protected const string DEVICE_ENDPOINT = "/clip/v2/resource/light/{0}";
    protected const string EVENTSTREAM_ENDPOINT = "/eventstream/clip/v2";
    #endregion

    #region Init
    public HueConnector(string? connectorSettingsJson,
        Func<IConnector, string, DeviceType, Task<IDevice?>> getDeviceAsync,
        Func<List<IDeviceChangeArgs>, Task> onDeviceChangedAsync,
        IDeviceFactory deviceFactory,
        IDeviceChangeArgsFactory deviceChangeArgsFactory,
        IDataProtectionService dataProtectionService,
        IStringLocalizer localizer,
        ILogger logger)
    {
        Settings = String.IsNullOrEmpty(connectorSettingsJson) ? null : JsonSerializer.Deserialize<HueConnectorSettings?>(connectorSettingsJson);
        DeviceFactory = deviceFactory;
        DeviceChangeArgsFactory = deviceChangeArgsFactory;
        DataProtectionService = dataProtectionService;
        Localizer = localizer;
        Logger = logger;
        GetDeviceAsync = getDeviceAsync;
        OnDeviceChangedAsync = onDeviceChangedAsync;

        if (Enabled)
        {
            ListenForDeviceUpdatesTaskTokenSource = new CancellationTokenSource();
            ListenForDeviceUpdatesTask = Task.Factory.StartNew(async () => await ListenForDeviceUpdatesAsync(ListenForDeviceUpdatesTaskTokenSource.Token).ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }
    }
    #endregion

    public async Task<(bool IsAvailable, string? ErrorMessage, DiscoverHueBridgeResponse? Bridge)> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var response = await client.GetFromJsonAsync<List<DiscoverHueBridgeResponse>>(new Uri(DISCOVER_HUE_BRIDGE_URL), cancellationToken).ConfigureAwait(false);
        var firstBridge = response?.FirstOrDefault();
        if (firstBridge == null || string.IsNullOrEmpty(firstBridge.InternalIpAddress) || firstBridge.Port == 0)
            return (false, Localizer["No Hue bridge found in the local network."], firstBridge);

        return (true, null, firstBridge);
    }

    public async Task<(bool Success, string? ErrorMessage, IConnectorSettings? Settings)> RegisterAsync(CancellationToken cancellationToken = default)
    {
        var (isAvailable, errorMessage, bridge) = await IsAvailableAsync(cancellationToken);
        if (!isAvailable || bridge == null || string.IsNullOrEmpty(bridge.InternalIpAddress) || bridge.Port == 0)
            return (false, errorMessage, null);

        var settings = new HueConnectorSettings
        {
            Ip = bridge.InternalIpAddress,
            Port = bridge.Port
        };

        var machineName = Environment.MachineName.Trim().Replace(" ", String.Empty);
        if (machineName.Length > 19)
            machineName = machineName[..19];
        var request = new GenerateHueApiKeyRequest
        {
            DeviceType = $"DigitalAssistant#{machineName}",
            GenerateClientKey = true
        };

#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
        using var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen
        using var client = new HttpClient(handler);
        var uri = GenerateHueApiUri(settings, GENERATE_HUE_API_KEY_ENDPOINT);
        var responseMessage = await client.PostAsJsonAsync(uri, request).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        var parsedResponse = await responseMessage.Content.ReadFromJsonAsync<List<GenerateHueApiKeyResponse>>().ConfigureAwait(false);
        var responseItem = parsedResponse?.FirstOrDefault();

        if (responseItem == null)
            return (false, Localizer["Can not get a valid json response from the hue {0} api endpoint.", uri], null);

        if (responseItem.Error != null && responseItem.Error.Type == 101)
            return (false, Localizer["Please press the link button on the hue bridge first.", uri], null);

        if (responseItem.Success == null || String.IsNullOrEmpty(responseItem.Success.UserName))
            return (false, Localizer["Unexpected error when retrieving the access token from the hue bridge{0}{0}: Type: {1}{0}Description: {2}.", Environment.NewLine, responseItem.Error?.Type ?? -1, responseItem.Error?.Description ?? string.Empty], null);

        settings.AccessKeyEncrypted = DataProtectionService.Protect(responseItem.Success.UserName);
        settings.ClientKeyEncrypted = DataProtectionService.Protect(responseItem.Success.ClientKey);
        Settings = settings;

        if (Enabled && ListenForDeviceUpdatesTask == null && ListenForDeviceUpdatesTaskTokenSource == null)
        {
            ListenForDeviceUpdatesTaskTokenSource = new CancellationTokenSource();
            ListenForDeviceUpdatesTask = Task.Factory.StartNew(async () => await ListenForDeviceUpdatesAsync(ListenForDeviceUpdatesTaskTokenSource.Token).ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }

        return (true, null, settings);
    }

    public Task DisableConnectorAsync(CancellationToken cancellationToken = default)
    {
        Settings = null;
        Client?.Dispose();
        Client = null;

        ListenForDeviceUpdatesTaskTokenSource?.Cancel();
        ListenForDeviceUpdatesTask = null;
        ListenForDeviceUpdatesTaskTokenSource = null;

        return Task.CompletedTask;
    }

    public async Task<List<IDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<GetDevicesResponse>(GET_DEVICES_ENDPOINT, cancellationToken).ConfigureAwait(false);
        if (response == null || response.Data == null || response.Data.Count == 0)
            return [];

        var devices = new List<IDevice>();
        foreach (var deviceDescription in response.Data)
        {
            var lightService = deviceDescription.Services.Where(service => service.Type == "light").FirstOrDefault();
            if (lightService == null || string.IsNullOrEmpty(lightService.Id) || string.IsNullOrEmpty(deviceDescription.MetaData?.Name))
                continue;

            var detailedDeviceData = await GetAsync<GetDetailedDeviceDataResponse>(string.Format(DEVICE_ENDPOINT, lightService.Id), cancellationToken).ConfigureAwait(false); ;
            var detailedDeviceInfo = detailedDeviceData?.Data?.FirstOrDefault();
            if (detailedDeviceInfo == null)
                continue;

            var colorTemperaturIsAdjustable = detailedDeviceInfo.ColorTemperature?.Schema != null && detailedDeviceInfo.ColorTemperature?.Schema?.Minimum != null && detailedDeviceInfo.ColorTemperature?.Schema?.Maximum != null;
            var colorIsAdjustable = detailedDeviceInfo.Color?.Gamut != null && detailedDeviceInfo.Color?.Xy != null;
            var additionalLightDeviceData = new AdditionalLightDeviceData()
            {
                Gamut = detailedDeviceInfo.Color?.Gamut
            };

            var lightDevice = DeviceFactory.CreateLightDevice(this, detailedDeviceInfo.Id, deviceDescription.MetaData.Name, DeviceStatus.Online,
                deviceDescription.ProductData?.Manufacturer ?? string.Empty, deviceDescription.ProductData?.ProductName ?? string.Empty,
                detailedDeviceInfo.On?.IsOn ?? false, detailedDeviceInfo.Dimming?.Brightness != null, detailedDeviceInfo.Dimming?.Brightness ?? 100,
                colorTemperaturIsAdjustable, detailedDeviceInfo.ColorTemperature?.Mirek, detailedDeviceInfo.ColorTemperature?.Schema?.Minimum ?? 0, detailedDeviceInfo.ColorTemperature?.Schema?.Maximum ?? 0,
                colorIsAdjustable, colorIsAdjustable ? HueColorConverter.XyToHexColor(detailedDeviceInfo.Color!.Xy, detailedDeviceInfo.Color!.Gamut!) : null,
                JsonSerializer.Serialize(additionalLightDeviceData)
            );

            devices.Add(lightDevice);
        }

        return devices;
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
                return (false, null);
            default:
                throw new NotImplementedException($"The device type \"{device.Type}\" is not implemented for the connector \"{Name}\"");
        }
    }

    protected async Task<(bool Success, string? ErrorMessage)> ExecuteLightActionAsync(ILightDevice device, LightActionArgs args, CancellationToken cancellationToken = default)
    {
        AdditionalLightDeviceData? additionalLightDeviceData = null;
        if (!String.IsNullOrEmpty(device.AdditionalJsonData))
            additionalLightDeviceData = JsonSerializer.Deserialize<AdditionalLightDeviceData?>(device.AdditionalJsonData);

        var request = new UpdateLightRequest()
        {
            On = args.On == null ? null : new On { IsOn = args.On.Value },
            Dimming = GetValidDimmingValue(device, args.Brightness),
            DimmingDelta = GetValidBrightnessDeltaValue(device, args.BrightnessDelta),
            ColorTemperature = GetValidColorTemperatureValue(device, args.ColorTemperature, args.ColorTemperatureColor),
            ColorTemperatureDelta = GetValidColorTemperatureDeltaValue(device, args.ColorTemperatureDelta),
            Color = args.Color == null || additionalLightDeviceData?.Gamut == null ? null : new HueColor { Xy = HueColorConverter.HexToXyColor(args.Color, additionalLightDeviceData.Gamut) }
        };

        if (request.Dimming?.Brightness != null && request.On == null)
            request.On = new() { IsOn = request.Dimming.Brightness != 0 };

        var deviceEntpoint = string.Format(DEVICE_ENDPOINT, device.InternalId);
        var response = await PutAsync<UpdateLightRequest, UpdateLightResponse>(deviceEntpoint, request, cancellationToken).ConfigureAwait(false);
        if (response == null || response.Errors != null || response.Data == null || response.Data?.Count == 0)
            return (false, string.Join(", ", response?.Errors?.Select(entry => entry.Description) ?? [Localizer["Can not get a response from the hue bridge."]]));

        return (true, null);
    }

    protected Dimming? GetValidDimmingValue(ILightDevice device, double? brightness)
    {
        if (brightness == null || !device.IsDimmable)
            return null;

        var validBrightness = Math.Max(0, Math.Min(100, brightness.Value));
        return new Dimming() { Brightness = validBrightness };
    }

    protected DimmingDelta? GetValidBrightnessDeltaValue(ILightDevice device, double? brightnessDelta)
    {
        if (brightnessDelta == null || !device.IsDimmable)
            return null;

        var positiveDelta = brightnessDelta > 0;
        return new DimmingDelta()
        {
            Action = positiveDelta ? DeltaAction.up : DeltaAction.down,
            BrightnessDelta = positiveDelta ? Math.Min(100 - device.Brightness, brightnessDelta.Value) : Math.Min(device.Brightness, Math.Abs(brightnessDelta.Value))
        };
    }

    protected ColorTemperature? GetValidColorTemperatureValue(ILightDevice device, int? colorTemperature, ColorTemperatureColor? colorTemperatureColor)
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

        return new ColorTemperature() { Mirek = validColorTemperature };
    }

    protected ColorTemperatureDelta? GetValidColorTemperatureDeltaValue(ILightDevice device, int? colorTemperatureDelta)
    {
        if (colorTemperatureDelta == null || !device.ColorTemperatureIsAdjustable)
            return null;

        var positiveDelta = colorTemperatureDelta > 0;
        return new ColorTemperatureDelta()
        {
            Action = positiveDelta ? DeltaAction.up : DeltaAction.down,
            MirekDelta = positiveDelta ?
                            Math.Min(device.MaximumColorTemperature - device.ColorTemperature ?? 0, colorTemperatureDelta.Value)
                            : Math.Min(device.ColorTemperature ?? 0 - device.MinimumColorTemperature, Math.Abs(colorTemperatureDelta.Value))
        };
    }

    #endregion

    public async Task ListenForDeviceUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrEmpty(Settings?.AccessKeyEncrypted))
            return;

#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
        using var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen
        using var infiniteClient = new HttpClient(handler);
        infiniteClient.DefaultRequestHeaders.Add("hue-application-key", DataProtectionService.Unprotect(Settings?.AccessKeyEncrypted));
        infiniteClient.Timeout = Timeout.InfiniteTimeSpan;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var uri = GenerateHueApiUri(Settings!, EVENTSTREAM_ENDPOINT);
                var stream = await infiniteClient.GetStreamAsync(uri, cancellationToken);
                using var streamReader = new StreamReader(stream);
                while (!streamReader.EndOfStream)
                {
                    var jsonEvent = await streamReader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonEvent))
                        continue;

                    var response = JsonSerializer.Deserialize<List<EventStreamResponse>>(jsonEvent);

                    if (response == null || response.Count == 0)
                        continue;

                    var deviceChangeArgs = new List<IDeviceChangeArgs>();
                    foreach (var responseItem in response)
                    {
                        foreach (var updateEntry in responseItem.Data)
                        {
                            switch (responseItem.Type)
                            {
                                case "add":
                                    var lightService = updateEntry.Services?.Where(service => service.Type == "light").FirstOrDefault();
                                    if (lightService == null || String.IsNullOrEmpty(lightService.Id) || String.IsNullOrEmpty(updateEntry.MetaData?.Name))
                                        continue;

                                    var detailedDeviceData = await GetAsync<GetDetailedDeviceDataResponse>(string.Format(DEVICE_ENDPOINT, lightService.Id), cancellationToken).ConfigureAwait(false); ;
                                    var detailedDeviceInfo = detailedDeviceData?.Data?.FirstOrDefault();
                                    if (detailedDeviceInfo == null)
                                        continue;

                                    var colorTemperaturIsAdjustable = detailedDeviceInfo.ColorTemperature?.Schema != null && detailedDeviceInfo.ColorTemperature?.Schema?.Minimum != null && detailedDeviceInfo.ColorTemperature?.Schema?.Maximum != null;
                                    var colorIsAdjustable = detailedDeviceInfo.Color?.Gamut != null && detailedDeviceInfo.Color?.Xy != null;
                                    var additionalLightDeviceData = new AdditionalLightDeviceData()
                                    {
                                        Gamut = detailedDeviceInfo.Color?.Gamut
                                    };

                                    var lightDevice = DeviceFactory.CreateLightDevice(this, lightService.Id, updateEntry.MetaData.Name, DeviceStatus.Online,
                                            updateEntry.ProductData?.Manufacturer ?? String.Empty, updateEntry.ProductData?.ProductName ?? String.Empty,
                                            detailedDeviceInfo.On?.IsOn ?? false, detailedDeviceInfo.Dimming?.Brightness != null, detailedDeviceInfo.Dimming?.Brightness ?? 100,
                                            colorTemperaturIsAdjustable, detailedDeviceInfo.ColorTemperature?.Mirek, detailedDeviceInfo.ColorTemperature?.Schema?.Minimum ?? 0, detailedDeviceInfo.ColorTemperature?.Schema?.Maximum ?? 0,
                                            colorIsAdjustable, colorIsAdjustable ? HueColorConverter.XyToHexColor(detailedDeviceInfo.Color!.Xy, detailedDeviceInfo.Color!.Gamut!) : null,
                                            JsonSerializer.Serialize(additionalLightDeviceData)
                                    );

                                    deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateAddDeviceArgs(this, lightDevice));
                                    break;

                                case "update":
                                    if (updateEntry.Type != "light" || String.IsNullOrEmpty(updateEntry.Id))
                                        continue;

                                    if (!String.IsNullOrEmpty(updateEntry.MetaData?.Name))
                                        deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateRenameDeviceArgs(this, updateEntry.Id, updateEntry.MetaData.Name));

                                    if (updateEntry.On?.IsOn == null &&
                                        updateEntry.Dimming?.Brightness == null &&
                                        updateEntry.ColorTemperature == null &&
                                        updateEntry.Color?.Xy == null)
                                        continue;

                                    var dbLightDevice = await GetDeviceAsync(this, updateEntry.Id, DeviceType.Light);
                                    AdditionalLightDeviceData? dbLightDeviceadditionalData = null;
                                    if (!String.IsNullOrEmpty(dbLightDevice?.AdditionalJsonData))
                                        additionalLightDeviceData = JsonSerializer.Deserialize<AdditionalLightDeviceData?>(dbLightDevice.AdditionalJsonData);

                                    var lightActionArgs = new LightActionArgs()
                                    {
                                        On = updateEntry.On?.IsOn,
                                        Brightness = updateEntry.Dimming?.Brightness,
                                        SetColorTemperature = updateEntry.ColorTemperature != null,
                                        ColorTemperature = updateEntry.ColorTemperature?.Mirek,
                                        Color = updateEntry.Color?.Xy != null ? HueColorConverter.XyToHexColor(updateEntry.Color.Xy, dbLightDeviceadditionalData?.Gamut ?? new()) : null
                                    };

                                    deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateUpdateDeviceArgs(this, updateEntry.Id, lightActionArgs));
                                    break;

                                case "delete":
                                    if (updateEntry.Type != "light" || String.IsNullOrEmpty(updateEntry.Id))
                                        continue;

                                    deviceChangeArgs.Add(DeviceChangeArgsFactory.CreateDeleteDeviceArgs(this, updateEntry.Id));
                                    break;
                            }
                        }
                    }

                    if (deviceChangeArgs.Count > 0)
                        await OnDeviceChangedAsync.Invoke(deviceChangeArgs);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while reading the event stream from the hue bridge.");
            }
        }
    }

    protected async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        InitClientIfNeeded();

        var uri = GenerateHueApiUri(Settings!, endpoint);
        return await Client!.GetFromJsonAsync<T>(uri, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        InitClientIfNeeded();

        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var uri = GenerateHueApiUri(Settings!, endpoint);
        var responseMessage = await Client!.PutAsJsonAsync(uri, request, jsonSerializerOptions).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        return await responseMessage.Content.ReadFromJsonAsync<TResponse>(cancellationToken).ConfigureAwait(false);
    }

    #region MISC

    protected void InitClientIfNeeded()
    {
        if (Settings == null)
            throw new Exception(Localizer["Hue connector settings not found. Please register the connector first."]);

        if (Client == null)
        {
#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
            var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.Add("hue-application-key", DataProtectionService.Unprotect(Settings.AccessKeyEncrypted));
        }
    }

    protected string GenerateHueApiUri(HueConnectorSettings settings, string endpoint)
    {
        return $"https://{settings.Ip}:{settings.Port}{endpoint}";
    }

    #endregion
}
