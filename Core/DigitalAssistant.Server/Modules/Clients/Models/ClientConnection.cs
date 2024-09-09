using DigitalAssistant.Base.ClientServerConnection;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;

namespace DigitalAssistant.Server.Modules.Clients.Models;

public record ClientConnection(TcpClient TcpClient, SslStream SslStream)
{
    #region Properties
    public Client? Client { get; set; }
    public bool ClientIsAuthenticated { get; set; }
    #endregion

    #region Member
    protected SemaphoreSlim MessageSemaphore { get; init; } = new(1, 1);
    protected SemaphoreSlim ResponseSemaphore { get; init; } = new(1, 1);
    protected ConcurrentDictionary<Guid, (DateTime AddTime, object? Data)> ResponseData { get; init; } = [];
    protected ConcurrentDictionary<Guid, TaskCompletionSource> EventsWaitingForResponse { get; init; } = [];
    #endregion

    public async Task<(bool Success, Exception? Error)> SendMessageToClientAsync(TcpMessage message, CancellationToken cancellationToken = default)
    {
        if (TcpClient == null || SslStream == null)
            return (false, null);

        await MessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            MessageSemaphore.Release();
        }
    }

    public async Task<bool> AddResponseDataAsync(Guid eventId, object? responseData, CancellationToken cancellationToken = default)
    {
        await ResponseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var success = ResponseData.TryAdd(eventId, (DateTime.Now, responseData));

            if (EventsWaitingForResponse.Remove(eventId, out var taskCompletionSource))
                taskCompletionSource.TrySetResult();

            return success;
        }
        finally
        {
            ResponseSemaphore.Release();
        }
    }

    public async Task<T?> GetResponseDataAsync<T>(Guid eventId, bool waitForResponse = true, int timeoutInMilliseconds = 30000, CancellationToken cancellationToken = default)
    {
        var taskCompletionSource = new TaskCompletionSource();

        await ResponseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CleanupOldResponseData();
            if (ResponseData.Remove(eventId, out var responseDataValue))
                return (T?)responseDataValue.Data;

            if (!waitForResponse)
                return default;

            if (!EventsWaitingForResponse.TryAdd(eventId, taskCompletionSource))
                return default;
        }
        finally
        {
            ResponseSemaphore.Release();
        }

        var timeoutTask = Task.Delay(timeoutInMilliseconds, cancellationToken);
        var completedTask = await Task.WhenAny(taskCompletionSource.Task, timeoutTask);
        bool timeout = completedTask == timeoutTask;
        EventsWaitingForResponse.Remove(eventId, out _);

        if (timeout)
            return default;

        return ResponseData.Remove(eventId, out var responseData) ? (T?)responseData.Data : default;
    }

    protected void CleanupOldResponseData()
    {
        var dataToRemove = ResponseData.Where(entry => entry.Value.AddTime < DateTime.Now.AddHours(-1));
        foreach (var item in dataToRemove)
            ResponseData.Remove(item.Key, out _);
    }
}
