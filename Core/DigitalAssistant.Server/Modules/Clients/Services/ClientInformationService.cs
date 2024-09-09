using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.General;
using System.Collections.Concurrent;

namespace DigitalAssistant.Server.Modules.Clients.Services;

public class ClientInformationService
{
    #region Members
    protected ConcurrentDictionary<Guid, ClientConnection> Clients { get; set; } = [];
    #endregion

    public bool AddClient(Guid clientId, ClientConnection client)
    {
        if (Clients.TryAdd(clientId, client))
        {
            if (!String.IsNullOrEmpty(client.Client?.Name))
                GlobalEventService.InvokeClientConnected(client.Client.Name);
            return true;
        }

        if (Clients.TryRemove(clientId, out var oldClientConnection))
        {
            if (oldClientConnection.TcpClient.Connected)
                try
                {
                    oldClientConnection.TcpClient.Close();
                }
                catch (Exception) { }

            if (!String.IsNullOrEmpty(oldClientConnection?.Client?.Name))
                GlobalEventService.InvokeClientDisconnected(oldClientConnection.Client.Name);
        }

        var success = Clients.TryAdd(clientId, client);

        if (success && !String.IsNullOrEmpty(client.Client?.Name))
            GlobalEventService.InvokeClientConnected(client.Client.Name);

        return success;
    }

    public bool RemoveClient(Guid clientId, ClientConnection oldClientConnection)
    {
        Clients.TryGetValue(clientId, out var currentClientConnection);
        if (currentClientConnection != oldClientConnection)
            return true;

        var success = Clients.TryRemove(clientId, out var clientConnection);

        if (!String.IsNullOrEmpty(clientConnection?.Client?.Name))
            GlobalEventService.InvokeClientDisconnected(clientConnection.Client.Name);

        return success;
    }

    public ClientConnection? GetClientConnection(Guid clientId)
    {
        if (Clients.TryGetValue(clientId, out var client))
            return client;

        return null;
    }
}
