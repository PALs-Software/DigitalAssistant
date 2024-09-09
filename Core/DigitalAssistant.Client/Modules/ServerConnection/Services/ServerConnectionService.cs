using DigitalAssistant.Base.ClientServerConnection;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;

namespace DigitalAssistant.Client.Modules.ServerConnection.Services;

public class ServerConnectionService
{
    #region Properties
    public TcpClient? TcpClient { get; set; }
    public SslStream? SslStream { get; set; }
    public bool ConnectionIsAuthenticated { get; set; }
    public SecureString? ServerAccessToken { get; set; }
    #endregion

    #region Member
    protected SemaphoreSlim Semaphore { get; init; } = new(1, 1);
    
    #endregion

    public async Task<(bool Success, Exception? Error)> SendMessageToServerAsync(TcpMessage message, CancellationToken cancellationToken = default)
    {
        if (TcpClient == null || SslStream == null)
            return (false, null);

        if (!ConnectionIsAuthenticated && message.Type != TcpMessageType.Authentication)
            return (false, null);

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!TcpClient.Connected)
                return (false, null);

            await SslStream.WriteAsync(message.GetMessageHeaderBytes(), cancellationToken);
            await SslStream.WriteAsync(message.Data, cancellationToken);
            await SslStream.FlushAsync(cancellationToken);

            return (true, null);
        }
        catch (Exception e)
        {
            return (false, e);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
