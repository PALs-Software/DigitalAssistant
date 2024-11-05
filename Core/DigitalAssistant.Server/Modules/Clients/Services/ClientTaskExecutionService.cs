using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.Abstractions.General.Extensions;
using BlazorBase.AudioRecorder.Services;
using BlazorBase.CRUD.Extensions;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Commands.Abstracts;
using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.ClientServerConnection.MessageTransferModels;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Ai.Asr.Services;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Services;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Enums;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Clients.Models.ClientTasks;
using DigitalAssistant.Server.Modules.Commands.Services;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Clients.Services;

public class ClientTaskExecutionService(IServiceProvider serviceProvider,
                                        IBaseDbContext baseDbContext,
                                        CommandProcessor commandProcessor,
                                        AsrService asrService,
                                        TtsService ttsService,
                                        ClientInformationService clientInformationService,
                                        AudioService audioService,
                                        AudioConverter audioConverter,
                                        ILogger<ClientTaskExecutionService> logger,
                                        BaseErrorService baseErrorService
    ) : TimerBackgroundService(logger, baseErrorService)
{
    protected override TimeSpan TimerInterval => TimeSpan.FromMilliseconds(25);

    #region Injects
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly IBaseDbContext DbContext = baseDbContext;
    protected readonly CommandProcessor CommandProcessor = commandProcessor;
    protected readonly AsrService AsrService = asrService;
    protected readonly TtsService TextToSpeechService = ttsService;
    protected readonly ClientInformationService ClientInformationService = clientInformationService;
    protected readonly AudioService AudioService = audioService;
    protected readonly AudioConverter AudioConverter = audioConverter;
    
    #endregion

    #region Member
    protected ConcurrentQueue<ClientTcpMessage> ClientMessages { get; set; } = new();
    protected ConcurrentDictionary<Guid, List<ClientTask>> ClientTasks { get; set; } = new();
    protected JsonSerializerOptions JsonSerializerOptions = new() { IncludeFields = true };

    protected Int16[] AudioConvertInt16Buffer = new Int16[SAMPLE_RATE * 5];
    protected readonly object AudioConvertLock = new();
    #endregion

    #region Constants
    protected const int SAMPLE_RATE = 16000;
    #endregion

    public void ScheduleClientMessage(ClientTcpMessage message)
    {
        ClientMessages.Enqueue(message);
    }

    protected override async Task OnTimerElapsedAsync(CancellationToken stoppingToken)
    {
        if (ClientMessages.Count == 0)
            return;

        while (ClientMessages.TryDequeue(out var message))
        {
            if ((!message.ClientConnection.ClientIsAuthenticated || message.ClientConnection.Client == null) &&
                message.Type != TcpMessageType.Authentication &&
                message.Type != TcpMessageType.AvailableClientToSetup &&
                message.Type != TcpMessageType.SetupClientWithServer)
            {
                CloseTcpConnection(message, $"Client \"{message.ClientConnection.TcpClient.Client.RemoteEndPoint as IPEndPoint}\" is not authenticated to use this method \"{message.Type}\", client connection will be closed");
                continue;
            }

            switch (message.Type)
            {
                case TcpMessageType.AvailableClientToSetup:
                    ProcessAvailableClientToSetup(message);
                    break;
                case TcpMessageType.SetupClientWithServer:
                    await ProcessSetupClientWithServerAsync(message);
                    break;
                case TcpMessageType.Authentication:
                    await ProcessAuthenticationMessageAsync(message);
                    break;
                case TcpMessageType.AudioData:
                    await ProcessAudioDataMessageAsync(message);
                    break;
                case TcpMessageType.Action:
                    await ProcessClientActionResponseMessageAsync(message);
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

    protected void ProcessAvailableClientToSetup(ClientTcpMessage message)
    {
        message.ClientConnection.IsAvailableClientForSetup = true;
        message.ClientConnection.Client = new Client { Id = Guid.NewGuid(), Name = Encoding.UTF8.GetString(message.Data) };
        ClientInformationService.AddClient(message.ClientConnection.Client.Id, message.ClientConnection);
    }

    protected async Task ProcessSetupClientWithServerAsync(ClientTcpMessage message)
    {
        await message.ClientConnection.AddResponseDataAsync(message.EventId, message.Data[0]);
    }

    protected async Task ProcessAuthenticationMessageAsync(ClientTcpMessage message)
    {
        var tcpClient = message.ClientConnection.TcpClient;
        var token = Encoding.UTF8.GetString(message.Data);
        var hash = Base.Extensions.StringExtension.CreateSHA512Hash(token);
        if (!Cache.ClientCache.Clients.TryGetValue(hash, out Client? cachedClient))
        {
            CloseTcpConnection(message, $"Invalid access token from client \"{tcpClient.Client.RemoteEndPoint as IPEndPoint}\", client connection will be closed");
            return;
        }

        if (cachedClient.ValidUntil < DateTime.Now)
        {
            CloseTcpConnection(message, $"Access token from client \"{tcpClient.Client.RemoteEndPoint as IPEndPoint}\" is not valid anymore, client connection will be closed");
            return;
        }

        if (!cachedClient.HasBeenInitialized)
        {
            var clientEntry = await DbContext.FindAsync<Client>(cachedClient.Id);
            if (clientEntry == null)
            {
                CloseTcpConnection(message, $"Cant find cached client \"{cachedClient.Name}\" in the database");
                return;
            }

            var finalToken = TokenService.GenerateRandomToken(128);
            await message.ClientConnection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.Authentication, Guid.NewGuid(), Encoding.UTF8.GetBytes(finalToken)));

            clientEntry.HasBeenInitialized = true;
            clientEntry.TokenHash = finalToken.CreateSHA512Hash();
            await DbContext.SaveChangesAsync();

            cachedClient = await DbContext.FirstAsync<Client>(entry => entry.Id == clientEntry.Id, asNoTracking: true);
            Cache.ClientCache.Clients.TryRemove(hash, out _);
            Cache.ClientCache.Clients[clientEntry.TokenHash] = cachedClient;
        }

        message.ClientConnection.ClientIsAuthenticated = true;
        message.ClientConnection.Client = cachedClient;
        ClientInformationService.AddClient(cachedClient.Id, message.ClientConnection);

        if (!cachedClient.ClientNeedSettingsUpdate)
            return;
        
        var localDbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var client = await localDbContext.FindAsync<Client>(cachedClient.Id);
        if (client == null)
            return;

        var eventId = Guid.NewGuid();
        var clientSettings = new ClientSettings();
        client.TransferPropertiesTo(clientSettings);
        var settingsBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(clientSettings));

        // Already disable the setting so in the client restart it will not be done twice, and reset if if the update fails
        client.ClientNeedSettingsUpdate = false;
        await localDbContext.SaveChangesAsync();
        await client.UpdateClientCacheAsync(localDbContext);

        await message.ClientConnection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.UpdateClientSettings, eventId, settingsBytes));

        _ = Task.Run(async () =>
        {
            var success = await message.ClientConnection.GetResponseDataAsync<bool>(eventId, timeoutInMilliseconds: 5000);
            if (success)
                return;

            client.ClientNeedSettingsUpdate = true;
            await localDbContext.SaveChangesAsync();
            await client.UpdateClientCacheAsync(DbContext);
        });
    }

    protected async Task ProcessAudioDataMessageAsync(ClientTcpMessage message)
    {
        var client = message.ClientConnection.Client;
        if (client == null)
            return;

        if (client.LastProcessedAudioMessageEventId == message.EventId)
            return;

        if (!ClientTasks.TryGetValue(client.Id, out var openTasks))
        {
            openTasks = [];
            ClientTasks.TryAdd(client.Id, openTasks);
        }

        var commandTask = openTasks.Where(entry => entry.Type == ClientTaskType.Command).FirstOrDefault();
        if (commandTask == null)
        {
            commandTask = new CommandClientTask(client, ClientTaskType.Command);
            openTasks.Add(commandTask);
        }

        var commandClientTask = (CommandClientTask)commandTask;
        if (client.Type == ClientType.Microcontroller)
            AddByteInt16SamplesToCommandClientTask(commandClientTask, message.Data);
        else
            commandClientTask.AudioData.AddRangeBinary(message.Data, sizeof(float));

        var speakerHasFinished = AudioService.SpeakerFinishedSpeaking(commandClientTask.AudioData.AsSpan(), SAMPLE_RATE, threshold: 40, maxDetectionDurationInSeconds: 15);
        if (!speakerHasFinished)
            return;

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Speaker finished speaking, start processing...");

        await message.ClientConnection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.StopSendingAudioData, message.EventId, [])).ConfigureAwait(false);
        openTasks.Remove(commandTask);
        client.LastProcessedAudioMessageEventId = message.EventId;

        _ = Task.Factory.StartNew(async () =>
        {
            AudioService.NormalizeAudioData(commandClientTask.AudioData.AsSpan());
            var result = await AsrService.ConvertSpeechToTextAsync(commandClientTask.AudioData.AsMemory(), SAMPLE_RATE).ConfigureAwait(false);
            if (result == null)
                return;

            var clientBase = new ClientBase(client);
            var culturInfo = CultureInfo.GetCultureInfo((int?)Cache.SetupCache.Setup?.InterpreterLanguage ?? 9);
            var commandResponse = await CommandProcessor.ProcessUserCommandAsync(result, culturInfo.TwoLetterISOLanguageName, clientBase, ServiceProvider).ConfigureAwait(false);

            if (String.IsNullOrEmpty(commandResponse))
                return;
            var audioResponse = await TextToSpeechService.ConvertTextToSpeechAsync(commandResponse).ConfigureAwait(false);
            if (audioResponse == null)
                return;

            byte[] audioResponseBytes;
            if (client.Type == ClientType.Microcontroller)
            {
                var resampledResponse = AudioConverter.Resample(audioResponse.AsSpan(), TextToSpeechService.GetCurrentModelSampleRate().GetValueOrDefault(22050), 16000);
                var shorts = AudioConverter.ConvertFloatToShortSamples(resampledResponse, withScaling: true);
                audioResponseBytes = new byte[shorts.Length * 2];
                Buffer.BlockCopy(shorts, 0, audioResponseBytes, 0, audioResponseBytes.Length);
            }                
            else
                audioResponseBytes = audioResponse.ToByteArray(sizeof(float));

            await message.ClientConnection.SendMessageToClientAsync(new TcpMessage(TcpMessageType.AudioData, message.EventId, audioResponseBytes)).ConfigureAwait(false);
        });
    }

    protected async Task ProcessClientActionResponseMessageAsync(ClientTcpMessage message)
    {
        if (message.ClientConnection.Client == null || message.Data == null || message.Data.Length == 0)
            return;

        var json = Encoding.UTF8.GetString(message.Data);
        var response = JsonSerializer.Deserialize<ClientActionResponse>(json, JsonSerializerOptions);
        await message.ClientConnection.AddResponseDataAsync(message.EventId, response);
    }

    protected async Task ProcessTransferAudioDevicesMessageAsync(ClientTcpMessage message)
    {
        if (message.ClientConnection.Client == null || message.Data == null || message.Data.Length == 0)
            return;

        var clientDevicesJson = Encoding.UTF8.GetString(message.Data);
        var clientDevices = JsonSerializer.Deserialize<ClientDevices>(clientDevicesJson, JsonSerializerOptions);
        await message.ClientConnection.AddResponseDataAsync(message.EventId, clientDevices);
    }

    protected async Task ProcessUpdateClientSettingsMessageAsync(ClientTcpMessage message)
    {
        if (message.ClientConnection.Client == null || message.Data == null || message.Data.Length == 0)
            return;

        var success = message.Data.Length == 1 && message.Data[0] == 1;
        await message.ClientConnection.AddResponseDataAsync(message.EventId, success);
    }

    #region MISC
    protected void CloseTcpConnection(ClientTcpMessage message, string warningMessage)
    {
        Logger.LogWarning(warningMessage);
        message.ClientConnection.SslStream.Close();
        message.ClientConnection.TcpClient.Close();
    }

    protected void AddByteInt16SamplesToCommandClientTask(CommandClientTask commandClientTask, byte[] data)
    {
        lock (AudioConvertLock)
        {
            var noOfSamplesRead = data.Length / 2;
            var bytesProcessed = 0;

            do
            {
                var bytesToConvert = Math.Min(data.Length - bytesProcessed, AudioConvertInt16Buffer.Length * 2);
                Buffer.BlockCopy(data, bytesProcessed, AudioConvertInt16Buffer, 0, bytesToConvert);
                bytesProcessed += bytesToConvert;
                var samplesConverted = bytesToConvert / 2;

                for (int i = 0; i < samplesConverted; i++)
                    commandClientTask.AudioData.Add(AudioConvertInt16Buffer[i]);

            } while (bytesProcessed < data.Length);
        }
    }
    #endregion
}
