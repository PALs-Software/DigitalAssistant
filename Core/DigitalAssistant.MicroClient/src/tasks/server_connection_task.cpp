#include "server_connection_task.h"

ServerConnectionTask *ServerConnectionTask::instance;

byte *ServerConnectionTask::access_token;
size_t ServerConnectionTask::access_token_length;
String ServerConnectionTask::host;
String ServerConnectionTask::server_certificate;
SemaphoreHandle_t ServerConnectionTask::udp_semaphore;

void ServerConnectionTask::Start(void *pvParameters)
{
    void **parameters = (void **)pvParameters;
    instance = (ServerConnectionTask *)parameters[0];
    instance->server_task_executer = (ServerTaskExecuter *)parameters[1];

    instance->Setup();
    instance->Run();
}

void ServerConnectionTask::Setup()
{
    DebugPrintln("Setup server connection task");
    buffer = (byte *)ps_malloc(CONNECTION_BUFFER_SIZE);

    udp_semaphore = xSemaphoreCreateMutex();
    host = String(Settings::GetServerAddress());
    char *temp_cert = Settings::GetServerCertificate();
    server_certificate = String(temp_cert);
    delete temp_cert;

    access_token = Settings::GetAccessToken(access_token_length);
    if (access_token_length == 0)
        access_token = nullptr;
}

void ServerConnectionTask::Run()
{
    while (true)
    {
        if (!EnsureClientIsConnected())
        {
            vTaskDelay(pdMS_TO_TICKS(5000));
            continue;
        }

        if (!authenticated)
            if (access_token == nullptr)
                SendAvailableClientToSetupMessageToServer();
            else
                SendAuthenticationMessageToServer();

        ProcessIncomingRequests();

        vTaskDelay(pdMS_TO_TICKS(25));
    }
}

bool ServerConnectionTask::EnsureClientIsConnected()
{
    if (WiFi.status() != WL_CONNECTED)
    {
        DebugPrintln("Cannot connect to the server because wifi is not connected, wait until connection is established...");
        return false;
    }

    if (xSemaphoreTake(udp_semaphore, portMAX_DELAY) != pdTRUE)
        return false;
    bool is_not_configured = host.isEmpty() || server_certificate.isEmpty();
    xSemaphoreGive(udp_semaphore);

    if (is_not_configured)
    {
        DiscoverServer();
        return false;
    }

    if (udp_client != nullptr)
    {
        udp_client->close();
        delete udp_client;
        udp_client = nullptr;
    }

    if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
        return false;

    if (client.connected())
    {
        xSemaphoreGive(semaphore);
        return true;
    }

    authenticated = false;
    DebugPrintln("Connect to server...");
    client.setCACert(server_certificate.c_str());
    if (client.connect(host.c_str(), port))
    {
        xSemaphoreGive(semaphore);
        DebugPrintln("Connected");
        return true;
    }

    xSemaphoreGive(semaphore);
    DebugPrintln("Cannot connect to the server, connection error...");
    return false;
}

void ServerConnectionTask::ProcessIncomingRequests()
{

    if (xSemaphoreTake(semaphore, portMAX_DELAY) == pdTRUE)
    {
        bytes_processed = 0;
        bytes_read = client.read(buffer, CONNECTION_BUFFER_SIZE);
        xSemaphoreGive(semaphore);
    }
    else
        bytes_read = 0;

    if (bytes_read <= 0)
        return;

    DebugPrintf("Read %d bytes from the server\n", bytes_read);

    // Message always either arrives in full or tcp connection crashes and a new connection is established and everything starts all over again
    // So it is sufficient to check for the message header in this way
    while (bytes_processed < bytes_read)
    {
        DebugPrintf("Bytes processed: %d\n", bytes_processed);
        int bytes_left_to_process = bytes_read - bytes_processed;

        // Check if the message header is not yet fully read & the remaining bytes are not enough to complete the message header
        if (current_message == nullptr && (bytes_left_to_process + message_header_buffer_bytes_read) < TcpMessage::MESSAGE_HEADER_BYTE_LENGTH)
        {
            std::memcpy(message_header_buffer + message_header_buffer_bytes_read, buffer + bytes_processed, bytes_left_to_process);
            message_header_buffer_bytes_read += bytes_left_to_process;
            bytes_processed = bytes_read;
            continue;
        }

        // Read the rest of the message header
        if (current_message == nullptr)
        {
            int remaining_header_bytes = TcpMessage::MESSAGE_HEADER_BYTE_LENGTH - message_header_buffer_bytes_read;
            std::memcpy(message_header_buffer + message_header_buffer_bytes_read, buffer + bytes_processed, remaining_header_bytes);
            bytes_processed += remaining_header_bytes;
            current_message = TcpMessage::CreateNewMessageFromHeaderData(message_header_buffer);
            DebugPrintf("Created new message: %d\n", current_message->GetType());
        }

        // If we have more bytes to process copy the remaining bytes to the message data body
        if (bytes_processed < bytes_read)
        {
            int bytes_to_read = std::min(bytes_read - bytes_processed, static_cast<int>(current_message->GetDataLength() - message_data_bytes_processed));
            std::memcpy(current_message->GetData() + message_data_bytes_processed, buffer + bytes_processed, bytes_to_read);
            bytes_processed += bytes_to_read;
            message_data_bytes_processed += bytes_to_read;
        }

        // If the message data is fully read, invoke callback and reset variables so that the next message can be processed
        if (current_message->GetDataLength() == message_data_bytes_processed)
        {
            server_task_executer->ScheduleServerMessage(current_message);

            current_message = nullptr;
            message_data_bytes_processed = 0;
            message_header_buffer_bytes_read = 0;
        }
    }

    DebugPrintf("Read done\n", bytes_read);
}

bool ServerConnectionTask::SendMessageToServer(TcpMessage *message)
{
    try
    {
        if (!authenticated &&
            message->GetType() != TcpMessageType::AvailableClientToSetup &&
            message->GetType() != TcpMessageType::SetupClientWithServer &&
            message->GetType() != TcpMessageType::Authentication)
            return false;

        if (!client.connected())
            return false;

        if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
            return false;

        byte message_header_bytes[TcpMessage::MESSAGE_HEADER_BYTE_LENGTH];
        message->GetMessageHeaderBytes(message_header_bytes);

        byte *data_transfer_pointer = message_header_bytes;
        size_t write_attempts = 0;
        size_t bytes_written = 0;
        size_t bytes_written_total = 0;
        do
        {
            bytes_written = client.write(data_transfer_pointer, TcpMessage::MESSAGE_HEADER_BYTE_LENGTH - bytes_written_total);
            bytes_written_total += bytes_written;
            data_transfer_pointer += bytes_written;
            write_attempts++;
        } while (bytes_written_total < TcpMessage::MESSAGE_HEADER_BYTE_LENGTH && write_attempts < 10);

        if (write_attempts >= 10)
        {
            DebugPrint("Unable to write header data to server, stop client and reconnect...");
            client.stop();
            return false;
        }

        int64_t data_length = message->GetDataLength();
        data_transfer_pointer = message->GetData();
        write_attempts = 0;
        bytes_written = 0;
        bytes_written_total = 0;
        do
        {
            bytes_written = client.write(data_transfer_pointer, data_length - bytes_written_total);
            bytes_written_total += bytes_written;
            data_transfer_pointer += bytes_written;
            write_attempts++;
        } while (bytes_written_total < data_length && write_attempts < 256);

        if (write_attempts >= 256)
        {
            DebugPrint("Unable to write body data to server, stop client and reconnect...");
            client.stop();            
            return false;
        }

        client.flush();

        xSemaphoreGive(semaphore);

        return true;
    }
    catch (...)
    {
        xSemaphoreGive(semaphore);
        DebugPrintln("Unkown exception by sending data to the server");
        return false;
    }
}

void ServerConnectionTask::DiscoverServer()
{
    DebugPrintln("Start discover server");

    int test = 0;
    if (udp_client == nullptr)
    {
        udp_client = new AsyncUDP();
        udp_client->listen(port);
        udp_client->onPacket([](AsyncUDPPacket packet)
                             {
            String data = String((char*)packet.data(), packet.length());
            DebugPrintf("Received udp package from %s with data: %s\n", packet.remoteIP().toString(), data.c_str());
            
            if (xSemaphoreTake(udp_semaphore, portMAX_DELAY) != pdTRUE)
                return;

            if (data.startsWith("GDASIA_RESPONSE:")) {
                host = data.substring(16);
                DebugPrintf("Got new server address '%s', now send request for certificate...\n", host.c_str());
                packet.printf("GetDigitalAssistantServerPublicCertificateKey");
            } else if (data.startsWith("GDASPCK_RESPONSE:")) {
                server_certificate = data.substring(17);
                DebugPrintf("Got new server certificate \n%s\n", server_certificate.c_str());
            } else {
                DebugPrintln("UDP package type is unkown");
            }

            xSemaphoreGive(udp_semaphore); });
    }

    udp_client->broadcastTo("GetDigitalAssistantServerIpAddress", port);
}

void ServerConnectionTask::SendAvailableClientToSetupMessageToServer()
{
    if (available_client_message_sent)
        return;
    DebugPrintln("Send available client message to server");

    guid_t event_id;
    create_guid(event_id);
    String client_name = "ESP32 Client";
    TcpMessage message(TcpMessageType::AvailableClientToSetup, event_id, (byte *)client_name.begin(), client_name.length());
    SendMessageToServer(&message);
    available_client_message_sent = true;
}

void ServerConnectionTask::SendAuthenticationMessageToServer()
{
    DebugPrintln("Send authentication message to server");

    guid_t event_id;
    create_guid(event_id);
    TcpMessage message(TcpMessageType::Authentication, event_id, access_token, access_token_length);
    SendMessageToServer(&message);
    authenticated = true;
}