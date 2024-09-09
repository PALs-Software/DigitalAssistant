using DigitalAssistant.Base;
using DigitalAssistant.Base.Extensions;
using DigitalAssistant.Base.BackgroundServiceAbstracts;
using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Base.General;
using DigitalAssistant.Client.Modules.ServerConnection.Models;
using DigitalAssistant.Client.Modules.General;
using DigitalAssistant.Abstractions.Services;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.AspNetCore.DataProtection;


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
    #endregion

    #region Members
    protected bool ClientIsInitialized;
    protected string ServerName;
    protected int ServerPort;
    protected bool ignoreServerCertificateErrors;
    protected TcpClient? Client;
    protected SslStream? SslStream;

    protected Task? ReadTask;
    #endregion

    public ServerConnectionHandler(IServiceProvider serviceProvider, IDataProtectionService dataProtectionService, ServerConnectionService serverConnectionService, ServerTaskExecutionService serverTaskExecutionService, IConfiguration configuration, ILogger<ServerConnectionHandler> logger, BaseErrorService baseErrorService) : base(logger, baseErrorService)
    {
        ServiceProvider = serviceProvider;
        DataProtectionService = dataProtectionService;
        ServerConnectionService = serverConnectionService;
        ServerTaskExecutionService = serverTaskExecutionService;

        ClientIsInitialized = configuration.GetValue<bool>("ClientIsInitialized");
        var settings = configuration.GetRequiredSection("ServerConnection").Get<ServerConnectionSettings>();
        ArgumentNullException.ThrowIfNull(settings);
        var serverAccessToken = settings.ServerAccessToken;
        ignoreServerCertificateErrors = settings.IgnoreServerCertificateErrors;
        ArgumentNullException.ThrowIfNull(serverAccessToken);

        if (!ClientIsInitialized)
            serverAccessToken = DataProtectionService.Protect(serverAccessToken);
        ServerConnectionService.ServerAccessToken = serverAccessToken?.ToSecureString();

        if (String.IsNullOrEmpty(settings.ServerName))
            throw new Exception("The configured \"ServerName\" must be a valid machine name or IP address.");

        if (settings.ServerPort < 1024 || settings.ServerPort > 65535)
            throw new Exception("The configured Port must be the configured port on the host machine and between 1024 and 65535.");

        ServerName = settings.ServerName;
        ServerPort = settings.ServerPort;

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

    protected override Task OnTimerElapsedAsync()
    {
        if (Client != null && Client.Connected)
            return Task.CompletedTask;

        return EstablishConnectionToServerAsync();
    }

    protected async Task<bool> EstablishConnectionToServerAsync()
    {
        try
        {
            var client = new TcpClient(ServerName, ServerPort);
            var sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            sslStream.AuthenticateAsClient(new SslClientAuthenticationOptions() { TargetHost = ServerName, EnabledSslProtocols = SslProtocols.Tls13 });

            ServerConnectionService.TcpClient = Client = client;
            ServerConnectionService.SslStream = SslStream = sslStream;

            var tokenBytes = Encoding.UTF8.GetBytes(DataProtectionService.Unprotect(ServerConnectionService.ServerAccessToken?.ToInsecureString()) ?? String.Empty);
            await ServerConnectionService.SendMessageToServerAsync(new TcpMessage(TcpMessageType.Authentication, Guid.NewGuid(), tokenBytes));

            if (ClientIsInitialized)
                ServerConnectionService.ConnectionIsAuthenticated = true;

            ClientIsInitialized = true;
            ReadTask = Task.Factory.StartNew(async () => await ProcessRequestsFromServerAsync(client, sslStream).ConfigureAwait(false), TaskCreationOptions.LongRunning);
        }
        catch (Exception e)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(e, "Error by connecting to the server {ServerName} on port {ServerPort}", ServerName, ServerPort);

            SslStream?.Close();
            Client?.Close();
            Client = null;
            SslStream = null;
            return false;
        }

        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Established connection to the server {ServerName} on port {ServerPort}", ServerName, ServerPort);

        return true;
    }

    protected bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (ignoreServerCertificateErrors)
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
}
