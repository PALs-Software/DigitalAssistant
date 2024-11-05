#pragma once
#include "config.h"
#include "modules/debug_printer.h"
#include <WiFiClientSecure.h>
#include <Wifi.h>
#include "modules/server_connection/tcp_message.h"
#include "modules/settings.h"
#include "modules/guid.h"
#include "server_task_executer.h"
#include <AsyncUDP.h>

class ServerConnectionTask
{
public:
    static void Start(void *pvParameter);
    static ServerConnectionTask* instance;

    static byte *access_token;
    static size_t access_token_length;
    static String host;
    static String server_certificate;
    static SemaphoreHandle_t udp_semaphore;

    bool SendMessageToServer(TcpMessage *message);

private:
    void Setup();
    void Run();

    bool EnsureClientIsConnected();
    void ProcessIncomingRequests();
    void DiscoverServer();

    void SendAvailableClientToSetupMessageToServer();
    void SendAuthenticationMessageToServer();

    WiFiClientSecure client;
    AsyncUDP *udp_client;
    ServerTaskExecuter *server_task_executer;

    int port = 59142;

    int bytes_read = 0;
    byte *buffer;

    byte message_header_buffer[TcpMessage::MESSAGE_HEADER_BYTE_LENGTH];
    int message_header_buffer_bytes_read = 0;

    TcpMessage *current_message = nullptr;
    int bytes_processed = 0;
    int message_data_bytes_processed = 0;

    bool authenticated = false;
    bool available_client_message_sent = false;

    SemaphoreHandle_t semaphore = xSemaphoreCreateMutex();
};