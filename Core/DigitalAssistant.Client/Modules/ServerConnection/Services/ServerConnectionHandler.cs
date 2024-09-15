using DigitalAssistant.Abstractions.Services;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.Extensions;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.General;
using DigitalAssistant.Client.Modules.ServerConnection.Models;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace DigitalAssistant.Client.Modules.ServerConnection.Services;

public class ServerConnectionHandler : TimerBackgroundService
{
#if DEBUG
    protected override TimeSpan TimerInterval => TimeSpan.FromSeconds(5);
#else
    protected override TimeSpan TimerInterval => TimeSpan.FromSeconds(30);
#endif

    #region Injects
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IDataProtectionService DataProtectionService;
    protected readonly ServerConnectionService ServerConnectionService;
    protected readonly ServerTaskExecutionService ServerTaskExecutionService;
    protected readonly ClientSettings ClientSettings;
    protected readonly ServerConnectionSettings ServerConnectionSettings;

    #endregion

    #region Members
    protected TcpClient? Client;
    protected SslStream? SslStream;

    protected Task? ReadTask;

    protected byte[] ServerDiscoveryRequestData = Encoding.UTF8.GetBytes("GetDigitalAssistantServerIpAddress");
    #endregion

    public ServerConnectionHandler(IServiceProvider serviceProvider, IDataProtectionService dataProtectionService, ServerConnectionService serverConnectionService, ServerTaskExecutionService serverTaskExecutionService, ClientSettings clientSettings, ServerConnectionSettings serverConnectionSettings, ILogger<ServerConnectionHandler> logger, BaseErrorService baseErrorService) : base(logger, baseErrorService)
    {
        ServiceProvider = serviceProvider;
        DataProtectionService = dataProtectionService;
        ServerConnectionService = serverConnectionService;
        ServerTaskExecutionService = serverTaskExecutionService;
        ClientSettings = clientSettings;
        ServerConnectionSettings = serverConnectionSettings;

        ArgumentNullException.ThrowIfNull(ServerConnectionSettings);
        ArgumentNullException.ThrowIfNull(ServerConnectionSettings.ServerPort);
        if (ServerConnectionSettings.ServerPort < 1024 || ServerConnectionSettings.ServerPort > 65535)
            throw new Exception("The configured Port must be the configured port on the host machine and between 1024 and 65535.");

        var serverAccessToken = String.IsNullOrWhiteSpace(ServerConnectionSettings.ServerAccessToken) ? null : ServerConnectionSettings.ServerAccessToken;
        if (!ClientSettings.ClientIsInitialized && !String.IsNullOrWhiteSpace(ServerConnectionSettings.ServerAccessToken) && !String.IsNullOrWhiteSpace(ServerConnectionSettings.ServerName))
            serverAccessToken = DataProtectionService.Protect(serverAccessToken);

        ServerConnectionSettings.SecureServerAccessToken = serverAccessToken?.ToSecureString();
        ServerConnectionSettings.ServerAccessToken = null;
        serverAccessToken = null;

        SoftRestartService.OnSoftRestart += async (sender, args) => await OnSoftRestartAsync();
    }

    protected virtual Task OnSoftRestartAsync()
    {
        SslStream?.Close();
        Client?.Close();
        Client = null;
        SslStream = null;

        return Task.CompletedTask;
    }

    protected override async Task OnTimerElapsedAsync(CancellationToken stoppingToken)
    {
        if (Client != null && Client.Connected)
            return;

        if (!ClientSettings.ClientIsInitialized && String.IsNullOrWhiteSpace(ServerConnectionSettings.ServerName))
            await DiscoverServerAsync(stoppingToken).ConfigureAwait(false);

        await EstablishConnectionToServerAsync().ConfigureAwait(false);
    }

    #region Tcp Client Communication

    protected async Task<bool> EstablishConnectionToServerAsync()
    {
        try
        {
            var client = new TcpClient(ServerConnectionSettings.ServerName!, ServerConnectionSettings.ServerPort);
            var sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            sslStream.AuthenticateAsClient(new SslClientAuthenticationOptions() { TargetHost = ServerConnectionSettings.ServerName, EnabledSslProtocols = SslProtocols.Tls13 });

            ServerConnectionService.TcpClient = Client = client;
            ServerConnectionService.SslStream = SslStream = sslStream;

            if (ServerConnectionSettings.SecureServerAccessToken != null)
            {
                var tokenBytes = Encoding.UTF8.GetBytes(DataProtectionService.Unprotect(ServerConnectionSettings.SecureServerAccessToken?.ToInsecureString()) ?? String.Empty);
                await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.Authentication, Guid.NewGuid(), tokenBytes));
                if (ClientSettings.ClientIsInitialized)
                    ServerConnectionService.ConnectionIsAuthenticated = true;
            }
            else
            {
                var machineNameBytes = Encoding.UTF8.GetBytes(Environment.MachineName);
                await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.AvailableClientToSetup, Guid.NewGuid(), machineNameBytes));
            }

            ReadTask = Task.Factory.StartNew(async () => await ProcessRequestsFromServerAsync(client, sslStream).ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }
        catch (Exception e)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(e, "Error by connecting to the server {ServerName} on port {ServerPort}", ServerConnectionSettings.ServerName, ServerConnectionSettings.ServerPort);

            SslStream?.Close();
            Client?.Close();
            Client = null;
            SslStream = null;
            return false;
        }

        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Established connection to the server {ServerName} on port {ServerPort}", ServerConnectionSettings.ServerName, ServerConnectionSettings.ServerPort);

        return true;
    }

    protected bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (ServerConnectionSettings.IgnoreServerCertificateErrors)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning("Client is configured to ignore server certificate errors. The following errors will be ignored: {SSLPolicyErrors}", sslPolicyErrors);
            return true;
        }

        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        if (Logger.IsEnabled(LogLevel.Error))
            Logger.LogError("Certificate errors by validating server certificate: {SSLPolicyErrors}", sslPolicyErrors);

        Environment.Exit(1);

        return false;
    }

    protected async Task ProcessRequestsFromServerAsync(TcpClient client, SslStream sslStream)
    {
        try
        {
            var tcpMessageHandler = ServiceProvider.GetRequiredService<TcpMessageHandler>();
            await tcpMessageHandler.ProcessIncomingRequestsAsync(client,
                sslStream,
                (message) => ServerTaskExecutionService.ScheduleServerMessage(new TcpMessage(message.Type, message.EventId, message.Data)))
                .ConfigureAwait(false);
        }
        catch (Exception) { }
        finally
        {
            sslStream.Close();
            client.Close();
        }
    }

    #endregion

    #region Server Discovery
    protected async Task DiscoverServerAsync(CancellationToken stoppingToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Start server discovery at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = new UdpClient
                {
                    EnableBroadcast = true
                };

                await client.SendAsync(ServerDiscoveryRequestData, ServerDiscoveryRequestData.Length, new IPEndPoint(IPAddress.Broadcast, ServerConnectionSettings.ServerPort)).ConfigureAwait(false);

                using CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(5000);
                var response = await client.ReceiveAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                var serverResponseData = Encoding.UTF8.GetString(response.Buffer);
                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("Server discovery received {serverRequest} from {remoteEndPointAddress}", serverResponseData, response.RemoteEndPoint.Address.ToString());

                client.Close();
                ServerConnectionSettings.ServerName = serverResponseData;
                break;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(e, "Error by processing server discovery");
            }
        }
    }
    #endregion
}
