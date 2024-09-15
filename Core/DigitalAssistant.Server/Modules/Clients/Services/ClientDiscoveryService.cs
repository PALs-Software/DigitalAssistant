using DigitalAssistant.Server.Modules.Clients.Models;
using System.Net.Sockets;
using System.Text;

namespace DigitalAssistant.Server.Modules.Clients.Services;

public class ClientDiscoveryService : BackgroundService
{
    #region Injects
    protected readonly ClientConnectionHandler ClientConnectionHandler;
    protected readonly ILogger<ClientDiscoveryService> Logger;
    #endregion

    #region Members
    protected int Port;
    #endregion

    public ClientDiscoveryService(ClientConnectionHandler clientConnectionHandler, IConfiguration configuration, ILogger<ClientDiscoveryService> logger)
    {
        ClientConnectionHandler = clientConnectionHandler;
        Logger = logger;

        var settings = configuration.GetRequiredSection("ClientConnection").Get<ClientConnectionSettings>();
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        if (settings.Port < 1024 || settings.Port > 65535)
            throw new Exception("The configured Port must be a free port on the host machine and between 1024 and 65535.");
        Port = settings.Port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Start client discovery service at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var server = new UdpClient(Port);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var request = await server.ReceiveAsync(stoppingToken);
                    var clientRequestData = Encoding.UTF8.GetString(request.Buffer);

                    if (Logger.IsEnabled(LogLevel.Information))
                        Logger.LogInformation("Client discovery service received {clientRequest} from {remoteEndPointAddress}, sending response", clientRequestData, request.RemoteEndPoint.Address.ToString());

                    if (clientRequestData != "GetDigitalAssistantServerIpAddress")
                        continue;

                    var responseData = Encoding.UTF8.GetBytes(ClientConnectionHandler.GetServerCertificateSubject());
                    await server.SendAsync(responseData, responseData.Length, request.RemoteEndPoint);
                }
            }
            catch (Exception e)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(e, "Error by processing client auto discovery");
            }
        }

    }
}