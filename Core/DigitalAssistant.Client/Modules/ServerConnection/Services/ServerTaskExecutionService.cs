﻿using BlazorBase.Abstractions.CRUD.Extensions;
using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.ClientServerConnection.MessageTransferModels;
using DigitalAssistant.Base.Extensions;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.Commands;
using DigitalAssistant.Client.Modules.General;
using DigitalAssistant.Client.Modules.ServerConnection.Models;
using DigitalAssistant.Client.Modules.SpeechRecognition.Services;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DigitalAssistant.Client.Modules.ServerConnection.Services;

public class ServerTaskExecutionService(
    ServerConnectionService serverConnectionService,
    ClientSettings settings,
    ServerConnectionSettings serverConnectionSettings,
    ClientState clientState,
    WakeWordListener wakeWordListener,
    SystemCommandHandler systemCommandHandler,
    MusicCommandHandler musicCommandHandler,
    TimerCommandHandler timerCommandHandler,
    IAudioPlayer audioPlayer,
    IAudioDeviceService audioDeviceService,
    IDataProtectionService dataProtectionService,
    ILogger<ServerTaskExecutionService> logger,
    BaseErrorService baseErrorService) : TimerBackgroundService(logger, baseErrorService)
{
    protected override TimeSpan TimerInterval => TimeSpan.FromMilliseconds(25);

    #region Injects
    protected readonly ServerConnectionService ServerConnectionService = serverConnectionService;
    protected readonly ClientSettings Settings = settings;
    protected readonly ServerConnectionSettings ServerConnectionSettings = serverConnectionSettings;
    protected readonly ClientState ClientState = clientState;
    protected readonly WakeWordListener WakeWordListener = wakeWordListener;
    protected readonly SystemCommandHandler SystemCommandHandler = systemCommandHandler;
    protected readonly MusicCommandHandler MusicCommandHandler = musicCommandHandler;
    protected readonly TimerCommandHandler TimerCommandHandler = timerCommandHandler;
    protected readonly IAudioPlayer AudioPlayer = audioPlayer;
    protected readonly IAudioDeviceService AudioDeviceService = audioDeviceService;
    protected readonly IDataProtectionService DataProtectionService = dataProtectionService;
    #endregion

    #region Member
    protected ConcurrentQueue<TcpMessage> ServerMessages = new();

    protected Task? TurnUpMusicStreamVolumeTask;
    protected JsonSerializerOptions JsonSerializerOptions = new() { IncludeFields = true };    
    #endregion

    public void ScheduleServerMessage(TcpMessage message)
    {
        ServerMessages.Enqueue(message);
    }

    protected override async Task OnTimerElapsedAsync(CancellationToken stoppingToken)
    {
        if (ServerMessages.IsEmpty)
            return;

        while (ServerMessages.TryDequeue(out var message))
        {
            switch (message.Type)
            {
                case TcpMessageType.SetupClientWithServer:
                    await ProcessSetupClientWithServerAsync(message);
                    break;
                case TcpMessageType.Authentication:
                    await ProcessAuthenticationMessageAsync(message);
                    break;
                case TcpMessageType.AudioData:
                    await ProcessAudioDataMessageAsync(message);
                    break;
                case TcpMessageType.StopSendingAudioData:
                    await ProcessStopSendingAudioDataMessageAsync(message);
                    break;
                case TcpMessageType.Action:
                    await ProcessActionMessageAsync(message);
                    break;
                case TcpMessageType.TransferAudioDevices:
                    await ProcessTransferAudioDevicesMessageAsync(message);
                    break;
                case TcpMessageType.UpdateClientSettings:
                    await ProcessUpdateClientSettingsMessageAsync(message);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    protected async Task ProcessSetupClientWithServerAsync(TcpMessage message)
    {
        try
        {
            var finalToken = Encoding.UTF8.GetString(message.Data).ToSecureString();
            var filePath = GetAppSettingsFilePath();
            var appSettingsJson = await File.ReadAllTextAsync(filePath);
            var jsonNode = JsonNode.Parse(appSettingsJson)!;

            jsonNode["ClientSettings"]!["ClientIsInitialized"] = true;
            jsonNode["ServerConnection"]!["ServerName"] = ServerConnectionSettings.ServerName;
            jsonNode["ServerConnection"]!["ServerAccessToken"] = DataProtectionService.Protect(finalToken.ToInsecureString());

            await File.WriteAllTextAsync(filePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            ServerConnectionSettings.SecureServerAccessToken = DataProtectionService.Protect(finalToken.ToInsecureString())?.ToSecureString();

            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.SetupClientWithServer, message.EventId, [(byte)ClientType.Computer + 1]));

            Settings.ClientIsInitialized = true;
            ServerConnectionService.ConnectionIsAuthenticated = true;
        }
        catch (Exception e)
        {
            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.SetupClientWithServer, message.EventId, [0]));

            Logger.LogError(e, "Error by connecting client with the server. Please make sure that the service has write access to the appsettings.json file and the appsettings.json is in a valid json format and contains all of the necessary base settings.");
            Environment.Exit(5);
        }
    }

    protected async Task ProcessAuthenticationMessageAsync(TcpMessage message)
    {
        try
        {
            var finalToken = Encoding.UTF8.GetString(message.Data).ToSecureString();
            var filePath = GetAppSettingsFilePath();
            var appSettingsJson = await File.ReadAllTextAsync(filePath);
            var jsonNode = JsonNode.Parse(appSettingsJson)!;

            jsonNode["ClientSettings"]!["ClientIsInitialized"] = true;
            jsonNode["ServerConnection"]!["ServerAccessToken"] = DataProtectionService.Protect(finalToken.ToInsecureString());

            await File.WriteAllTextAsync(filePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            ServerConnectionSettings.SecureServerAccessToken = DataProtectionService.Protect(finalToken.ToInsecureString())?.ToSecureString();
            ServerConnectionService.ConnectionIsAuthenticated = true;
            Settings.ClientIsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error by saving the final access token to the app settings. Please make sure that the service has write access to the appsettings.json file and the appsettings.json is in a valid json format and contains all of the necessary base settings.");
            Environment.Exit(5);
        }
    }

    protected async Task ProcessAudioDataMessageAsync(TcpMessage message)
    {
        if (AudioPlayer.IsPlaying(AudioType.Stream) && message.Data.Length > 0)
        {
            if (OperatingSystem.IsWindows())
            {
                AudioPlayer.SetVolume(AudioType.Stream, 0.2f);
                TurnUpMusicStreamVolumeTask = Task.Run(async () =>
                {
                    var speekingTime = message.Data.Length / sizeof(float) / Settings.VoiceAudioOutputSampleRate * 1000;
                    await Task.Delay(speekingTime + 250);
                    AudioPlayer.SetVolume(AudioType.Stream, 1f);
                    TurnUpMusicStreamVolumeTask = null;
                });
            }
            else
            {
                await AudioPlayer.PauseAsync(AudioType.Stream);
                TurnUpMusicStreamVolumeTask = Task.Run(async () =>
                {
                    var speekingTime = message.Data.Length / sizeof(float) / Settings.VoiceAudioOutputSampleRate * 1000;
                    await Task.Delay(speekingTime + 250);
                    await AudioPlayer.ResumeAsync(AudioType.Stream);
                    TurnUpMusicStreamVolumeTask = null;
                });
            }
        }

        await AudioPlayer.PlayAsync(message.Data);
    }

    protected Task ProcessStopSendingAudioDataMessageAsync(TcpMessage message)
    {
        WakeWordListener.StopAudioStreamToServer();
        return Task.CompletedTask;
    }

    protected async Task ProcessTransferAudioDevicesMessageAsync(TcpMessage message)
    {
        var clientDevices = new ClientDevices
        {
            OutputDevices = AudioDeviceService.GetOutputDevices(),
            InputDevices = AudioDeviceService.GetInputDevices()
        };

        var clientDevicesJson = JsonSerializer.Serialize(clientDevices, JsonSerializerOptions);
        var clientDevicesBytes = Encoding.UTF8.GetBytes(clientDevicesJson);
        await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.TransferAudioDevices, message.EventId, clientDevicesBytes));
    }

    protected async Task ProcessUpdateClientSettingsMessageAsync(TcpMessage message)
    {
        if (!Settings.ClientIsInitialized || message.Data == null || message.Data.Length == 0)
        {
            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, message.EventId, [0]));
            return;
        }

        var json = Encoding.UTF8.GetString(message.Data);
        var clientSettings = JsonSerializer.Deserialize<ClientSettings>(json);
        if (clientSettings == null)
        {
            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, message.EventId, [0]));
            return;
        }

        try
        {
            clientSettings.ClientIsInitialized = true;
            clientSettings.TransferPropertiesTo(Settings);
            var appsettingsFilePath = GetAppSettingsFilePath();
            var appSettingsJson = await File.ReadAllTextAsync(appsettingsFilePath);

            var jsonNode = JsonNode.Parse(appSettingsJson)!;
            var clientSettingsJson = jsonNode["ClientSettings"];
            clientSettingsJson ??= new JsonObject();
            clientSettingsJson.ReplaceWith(Settings);

            await File.WriteAllTextAsync(appsettingsFilePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver }));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error by saving the client settings in the app settings file. Please make sure that the service has write access to the appsettings.json file and the appsettings.json is in a valid json format and contains all of the necessary base settings.");
            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, message.EventId, [0]));
            return;
        }

        await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, message.EventId, [1]));
        SoftRestartService.ExecuteSoftRestart();
    }

    #region Process Action Types

    protected async Task ProcessActionMessageAsync(TcpMessage message)
    {
        var jsonTcpMessageAction = JsonObject.Parse(message.Data)?.AsObject();
        ArgumentNullException.ThrowIfNull(jsonTcpMessageAction);
        jsonTcpMessageAction.TryGetPropertyValue("Type", out var jsonActionType);
        jsonTcpMessageAction.TryGetPropertyValue("Language", out var jsonLanguage);
        ArgumentNullException.ThrowIfNull(jsonActionType);
        ArgumentNullException.ThrowIfNull(jsonLanguage);

        var actionType = jsonActionType.Deserialize<TcpMessageActionType>();
        var language = jsonLanguage.Deserialize<string>();
        ArgumentNullException.ThrowIfNull(language);

        var currentUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var response = actionType switch
            {
                TcpMessageActionType.SystemAction =>
                    await SystemCommandHandler.ProcessSystemActionAsync(DeserializeActionArgs<SystemActionArgs>(jsonTcpMessageAction)),

                TcpMessageActionType.MusicAction =>
                    await MusicCommandHandler.ProcessMusicActionAsync(DeserializeActionArgs<MusicActionArgs>(jsonTcpMessageAction)),

                TcpMessageActionType.TimerAction =>
                    await TimerCommandHandler.ProcessTimerActionAsync(DeserializeActionArgs<TimerActionArgs>(jsonTcpMessageAction)),

                _ => throw new NotImplementedException(),
            };

            var responseData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, JsonSerializerOptions));
            var tcpMessage = new TcpMessage(TcpMessageType.Action, message.EventId, responseData);
            await ServerConnectionService.SendMessageToServerAsync(tcpMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = currentUICulture;
        }
    }

    #endregion

    #region MISC
    protected TClientActionArgs DeserializeActionArgs<TClientActionArgs>(JsonObject jsonTcpMessageAction) where TClientActionArgs : class, IClientActionArgs
    {
        jsonTcpMessageAction.TryGetPropertyValue("Args", out var jsonArgs);
        var actionArgs = jsonArgs?.Deserialize<TClientActionArgs>();
        ArgumentNullException.ThrowIfNull(actionArgs);
        return actionArgs;
    }

    public string GetAppSettingsFilePath()
    {
#if DEBUG
        var appsettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.User.json");
#else
        var appsettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
#endif
        if (EnvironmentInformations.ApplicationRunsInDockerContainer)
            appsettingsFilePath = Path.Combine(AppContext.BaseDirectory, "DockerStorage", "appsettings.json");

        return appsettingsFilePath;
    }
    #endregion
}
