using BlazorBase.Abstractions.CRUD.Extensions;
using DigitalAssistant.Abstractions.Clients.Arguments;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Base;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.ClientServerConnection.MessageTransferModels;
using DigitalAssistant.Base.Extensions;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.Audio.Enums;
using DigitalAssistant.Client.Modules.Audio.Interfaces;
using DigitalAssistant.Client.Modules.General;
using DigitalAssistant.Client.Modules.ServerConnection.Models;
using DigitalAssistant.Client.Modules.SpeechRecognition.Services;
using DigitalAssistant.Client.Modules.State;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DigitalAssistant.Client.Modules.ServerConnection.Services;

public class ServerTaskExecutionService : TimerBackgroundService
{
    protected override TimeSpan TimerInterval => TimeSpan.FromMilliseconds(25);

    #region Injects
    protected readonly ServerConnectionService ServerConnectionService;
    protected readonly ClientSettings Settings;
    protected readonly ClientState ClientState;
    protected readonly WakeWordListener WakeWordListener;
    protected readonly IAudioPlayer AudioPlayer;
    protected readonly IAudioDeviceService AudioDeviceService;
    protected readonly IDataProtectionService DataProtectionService;
    #endregion

    #region Member
    protected ConcurrentQueue<TcpMessage> ServerMessages = new();
    protected List<ServerTask> ServerTasks = [];

    protected Task? TurnUpMusicStreamVolumeTask;
    protected JsonSerializerOptions JsonSerializerOptions = new() { IncludeFields = true };
    #endregion

    #region Init
    public ServerTaskExecutionService(ServerConnectionService serverConnectionService,
        ClientSettings settings,
        ClientState clientState,
        WakeWordListener wakeWordListener,
        IAudioPlayer audioPlayer,
        IAudioDeviceService audioDeviceService,
        IDataProtectionService dataProtectionService,
        ILogger<ServerTaskExecutionService> logger,
        BaseErrorService baseErrorService) : base(logger, baseErrorService)
    {
        ServerConnectionService = serverConnectionService;
        Settings = settings;
        ClientState = clientState;
        WakeWordListener = wakeWordListener;
        AudioPlayer = audioPlayer;
        AudioDeviceService = audioDeviceService;
        DataProtectionService = dataProtectionService;
    }
    #endregion

    public void ScheduleServerMessage(TcpMessage message)
    {
        ServerMessages.Enqueue(message);
    }

    protected override async Task OnTimerElapsedAsync()
    {
        if (ServerMessages.Count == 0)
            return;

        while (ServerMessages.TryDequeue(out var message))
        {
            switch (message.Type)
            {
                case TcpMessageType.Authentication:
                    await ProcessAuthenticationMessageAsync(message);
                    break;
                case TcpMessageType.AudioData:
                    await ProcessAudioDataMessageAsync(message);
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

    protected async Task ProcessAuthenticationMessageAsync(TcpMessage message)
    {
        try
        {
            var finalToken = Encoding.UTF8.GetString(message.Data).ToSecureString();
#if DEBUG
            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.User.json");
#else
            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
#endif
            var appSettingsJson = await File.ReadAllTextAsync(filePath);
            var jsonNode = JsonNode.Parse(appSettingsJson)!;

            jsonNode["ClientIsInitialized"] = true;
            jsonNode["ServerConnection"]!["ServerAccessToken"] = DataProtectionService.Protect(finalToken.ToInsecureString());

            await File.WriteAllTextAsync(filePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            ServerConnectionService.ServerAccessToken = DataProtectionService.Protect(finalToken.ToInsecureString())?.ToSecureString();
            ServerConnectionService.ConnectionIsAuthenticated = true;
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
        WakeWordListener.StopAudioStreamToServer();
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
        if (message.Data == null || message.Data.Length == 0)
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
            clientSettings.TransferPropertiesTo(Settings);
#if DEBUG
            var appsettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.User.json");
#else
            var appsettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
#endif
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

    protected Task ProcessActionMessageAsync(TcpMessage message)
    {
        var jsonTcpMessageAction = JsonObject.Parse(message.Data)?.AsObject();
        ArgumentNullException.ThrowIfNull(jsonTcpMessageAction);
        jsonTcpMessageAction.TryGetPropertyValue("Type", out var jsonActionType);
        ArgumentNullException.ThrowIfNull(jsonActionType);

        var actionType = jsonActionType.Deserialize<TcpMessageActionType>();

        return actionType switch
        {
            TcpMessageActionType.SystemAction =>
                ProcessSystemActionAsync(DeserializeActionArgs<SystemActionArgs>(jsonTcpMessageAction)),

            TcpMessageActionType.MusicAction =>
                ProcessMusicActionAsync(DeserializeActionArgs<MusicActionArgs>(jsonTcpMessageAction)),

            _ => throw new NotImplementedException(),
        };
    }


    protected async Task ProcessSystemActionAsync(SystemActionArgs args)
    {
        if (!args.StopCurrentAction.GetValueOrDefault())
            return;

        bool resumeStream = false;
        bool speechWasPlaying = false;
        if (AudioPlayer.IsPlaying(AudioType.SoundEffect))
        {
            await AudioPlayer.StopAsync(AudioType.SoundEffect);
            resumeStream = true;
        }

        if (AudioPlayer.IsPlaying(AudioType.Speech))
        {
            await AudioPlayer.StopAsync(AudioType.Speech);
            resumeStream = true;
            speechWasPlaying = true;
        }

        if (resumeStream)
        {
            if (OperatingSystem.IsWindows())
                AudioPlayer.SetVolume(AudioType.Stream, 1f);
            else
                await AudioPlayer.ResumeAsync(AudioType.Stream);
        }

        if (speechWasPlaying)
            return;

        if (AudioPlayer.IsPlaying(AudioType.Stream))
        {
            ClientState.StopLongRunningActionIfExists<MusicActionArgs>();
            await AudioPlayer.StopAsync(AudioType.Stream);
        }
    }

    protected Task ProcessMusicActionAsync(MusicActionArgs args)
    {
        if (String.IsNullOrEmpty(args.MusicStreamUrl))
            return Task.CompletedTask;

        ClientState.ReplaceLongRunningAction(args);
        return AudioPlayer.PlayAsync(args.MusicStreamUrl);
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
    #endregion
}
