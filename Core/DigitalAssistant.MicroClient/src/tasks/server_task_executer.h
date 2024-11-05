#pragma once
#include "config.h"
#include "modules/debug_printer.h"
#include "modules/server_connection/tcp_message.h"
#include "modules/server_connection/tcp_message_action_type.h"
#include "modules/settings.h"
#include <queue>
#include "ArduinoJson.h"

class ServerTaskExecuter
{
public:
    static void Start(void *pvParameter);

    void ScheduleServerMessage(TcpMessage *message);

private:
    void Setup();
    void Run();

    void ProcessSetupClientWithServer(TcpMessage *message);
    void ProcessAuthenticationMessage(TcpMessage *message);
    void ProcessAudioDataMessage(TcpMessage *message);
    void ProcessStopSendingAudioDataMessage(TcpMessage *message);
    void ProcessActionMessage(TcpMessage *message);
    void ProcessTransferAudioDevicesMessage(TcpMessage *message);
    void ProcessUpdateClientSettingsMessage(TcpMessage *message);

    void ProcessSystemAction();
    void ProcessTimerAction();

    static void PlayTimerSound(TimerHandle_t timer_handle);

    std::queue<TcpMessage *> server_messages;
    SemaphoreHandle_t semaphore = xSemaphoreCreateMutex();
    JsonDocument json_document;
    xTimerHandle timer_handle;
};