using DigitalAssistant.Base.ClientServerConnection;
using DigitalAssistant.Server.Modules.Clients.Models;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DigitalAssistant.Server.Modules.Clients.Services;

public class ClientConnectionHandler : BackgroundService
{
    #region Injects
    protected readonly TcpMessageHandler TcpMessageHandler;
    protected readonly ClientInformationService ClientInformationService;
    protected readonly ClientTaskExecutionService ClientTaskExecutionService;
    protected readonly ILogger<ClientConnectionHandler> Logger;
    #endregion

    #region Members
    protected int Port;
    protected X509Certificate ServerCertificate;
    protected List<Task> ReadTasks = [];
    #endregion

    public ClientConnectionHandler(TcpMessageHandler tcpMessageHandler, ClientInformationService clientInformationService, ClientTaskExecutionService clientTaskExecutionService, IConfiguration configuration, ILogger<ClientConnectionHandler> logger)
    {
        TcpMessageHandler = tcpMessageHandler;
        ClientInformationService = clientInformationService;
        ClientTaskExecutionService = clientTaskExecutionService;
        Logger = logger;

        var settings = configuration.GetRequiredSection("ClientConnection").Get<ClientConnectionSettings>();
        var certificateSettings = settings?.Certificate;
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(certificateSettings, nameof(certificateSettings));

        if (settings.Port < 1024 || settings.Port > 65535)
            throw new Exception("The configured Port must be a free port on the host machine and between 1024 and 65535.");
        Port = settings.Port;

        if (!String.IsNullOrEmpty(certificateSettings.Path))
        {
            X509Certificate? certificate;
            if (String.IsNullOrEmpty(certificateSettings.Password))
            {
                if (String.IsNullOrEmpty(certificateSettings.KeyPath))
                    certificate = new X509Certificate(certificateSettings.Path);
                else
                    certificate = X509Certificate2.CreateFromPemFile(certificateSettings.Path, certificateSettings.KeyPath);
            }
            else
                certificate = new X509Certificate2(certificateSettings.Path, certificateSettings.Password);

            if (certificate == null)
                throw new Exception("Can not get a valid certificate with the settings given for certificate path (and optionally password) in the appsettings.json file");

            ServerCertificate = certificate;
        }
        else if (certificateSettings.Location != null && !String.IsNullOrEmpty(certificateSettings.Store) && !String.IsNullOrEmpty(certificateSettings.Subject))
        {
            var store = new X509Store(certificateSettings.Store, certificateSettings.Location.Value);
            store.Open(OpenFlags.ReadOnly);
            var certificate = store.Certificates.Find(X509FindType.FindBySubjectName, certificateSettings.Subject, true).FirstOrDefault();
            store.Close();

            if (certificate == null)
                throw new Exception("Can not get a valid certificate with the settings given for location, store and subject in the appsettings.json file");

            ServerCertificate = certificate;
        }
        else
            throw new Exception("In the appsetting.json file is no valid certificate configuration. Please enter either a path and password or a location, store and subject.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Start client connection handler at: {time}", DateTimeOffset.Now);

        var tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync(stoppingToken);
            await ProcessClientAsync(client);

            var completedReadTasks = ReadTasks.Where(task => task.IsCompleted).ToList();
            foreach (var task in completedReadTasks)
                ReadTasks.Remove(task);
        }
    }

    protected async Task ProcessClientAsync(TcpClient tcpClient)
    {
        SslStream? sslStream = null;
        try
        {
            sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
            await sslStream.AuthenticateAsServerAsync(ServerCertificate,
                clientCertificateRequired: false,
                checkCertificateRevocation: true,
                enabledSslProtocols: SslProtocols.Tls13); // | SslProtocols.Tls12); // Also enable Tls12 for ESP32 because it does not support Tls13

            if (!sslStream.IsEncrypted)
                throw new Exception("Communication stream is not encrypted");

            ReadTasks.Add(await Task.Factory.StartNew(async () => await ProcessRequestsFromClientAsync(tcpClient, sslStream).ConfigureAwait(false), TaskCreationOptions.LongRunning));

            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("Successfully connected to client {ClientIpAddress}", tcpClient?.Client?.RemoteEndPoint as IPEndPoint ?? new IPEndPoint(0,0));
        }
        catch (Exception e)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(e, "Error by processing client {ClientIpAddress} connection", tcpClient?.Client?.RemoteEndPoint as IPEndPoint ?? new IPEndPoint(0, 0));
        }
    }

    protected async Task ProcessRequestsFromClientAsync(TcpClient tcpClient, SslStream sslStream)
    {
        var clientConnection = new ClientConnection(tcpClient, sslStream);
        try
        {            
            await TcpMessageHandler.ProcessIncomingRequestsAsync(tcpClient,
                sslStream,
                (message) => ClientTaskExecutionService.ScheduleClientMessage(new ClientTcpMessage(clientConnection, message.Type, message.EventId, message.Data)))
                .ConfigureAwait(false);
        }
        catch (Exception) { }
        finally
        {
            sslStream.Close();
            tcpClient.Close();
            if (clientConnection.Client?.Id != null)
                ClientInformationService.RemoveClient(clientConnection.Client.Id, clientConnection);
        }
    }

    #region Misc
    public string GetServerCertificateSubject()
    {
        return ServerCertificate.Subject[3..]; // Remove the "CN=" at the beginning
    }

    public string GetServerCertificatePublicKey()
    {
        var builder = new StringBuilder();

        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(ServerCertificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");

        return builder.ToString();
    }
    #endregion
}
