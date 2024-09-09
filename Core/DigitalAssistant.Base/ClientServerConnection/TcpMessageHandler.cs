using System.Net.Security;
using System.Net.Sockets;

namespace DigitalAssistant.Base.ClientServerConnection;

public class TcpMessageHandler
{
    public async Task ProcessIncomingRequestsAsync(TcpClient client, SslStream sslStream, Action<TcpMessage> onMessageReceived)
    {
        int bytesRead = 0;
        byte[] buffer = new byte[131072]; // 128 kb

        byte[] messageHeaderBuffer = new byte[TcpMessage.MessageHeaderByteLength];
        int messageHeaderBufferBytesRead = 0;

        TcpMessage? currentMessage = null;
        int bytesProcessed = 0;
        int messageDataBytesProcessed = 0;

        while (true)
        {
            if (!client.Connected)
                break;

            bytesProcessed = 0;
            bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            if (bytesRead == 0) // End of stream was reached -> client disconnected
                break;

            // Message always either arrives in full or tcp connection crashes and a new connection is established and everything starts all over again
            // So it is sufficient to check for the message header in this way
            while (bytesProcessed < bytesRead)
            {
                int bytesLeftToProcess = bytesRead - bytesProcessed;

                // Check if the message header is not yet fully read & the remaining bytes are not enough to complete the message header
                if (currentMessage == null && (bytesLeftToProcess + messageHeaderBufferBytesRead) < TcpMessage.MessageHeaderByteLength)
                {
                    Buffer.BlockCopy(buffer, bytesProcessed, messageHeaderBuffer, messageHeaderBufferBytesRead, bytesLeftToProcess);
                    messageHeaderBufferBytesRead += bytesLeftToProcess;
                    bytesProcessed = bytesRead;
                    continue;
                }

                // Read the rest of the message header
                if (currentMessage == null)
                {
                    Buffer.BlockCopy(buffer, bytesProcessed, messageHeaderBuffer, messageHeaderBufferBytesRead, messageHeaderBuffer.Length - messageHeaderBufferBytesRead);
                    bytesProcessed += messageHeaderBuffer.Length - messageHeaderBufferBytesRead;

                    (var messageType, var eventId, var messageLength) = TcpMessage.GetMessageHeaderData(messageHeaderBuffer);
                    currentMessage = new TcpMessage(messageType, eventId, new byte[messageLength]);
                }

                // If we have more bytes to process copy the remaining bytes to the message data body
                if (bytesProcessed < bytesRead)
                {
                    var bytesToRead = Math.Min(bytesRead - bytesProcessed, currentMessage.Data.Length - messageDataBytesProcessed);
                    Buffer.BlockCopy(buffer, bytesProcessed, currentMessage.Data, messageDataBytesProcessed, bytesToRead);
                    bytesProcessed += bytesToRead;
                    messageDataBytesProcessed += bytesToRead;
                }

                // If the message data is fully read, invoke callback and reset variables so that the next message can be processed
                if (currentMessage.Data.Length == messageDataBytesProcessed)
                {
                    onMessageReceived.Invoke(currentMessage);
                    currentMessage = null;
                    messageDataBytesProcessed = 0;
                    messageHeaderBufferBytesRead = 0;
                }
            }
        }
    }
}
