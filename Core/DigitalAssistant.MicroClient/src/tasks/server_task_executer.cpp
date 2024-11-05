#include "server_task_executer.h"
#include "server_connection_task.h"
#include "audio_player_task.h"
#include "wake_word_detection_task.h"

void ServerTaskExecuter::Start(void *pvParameters)
{
    ServerTaskExecuter *task = (ServerTaskExecuter *)pvParameters;
    task->Setup();
    task->Run();
}

void ServerTaskExecuter::Setup()
{
    DebugPrintln("Setup server task executer");

    timer_handle = xTimerCreate("timer", pdMS_TO_TICKS(1000), pdFALSE, 0, &ServerTaskExecuter::PlayTimerSound);
}

void ServerTaskExecuter::ScheduleServerMessage(TcpMessage *message)
{
    DebugPrintln("Schedule server message for execution...");

    if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
        return;

    server_messages.push(message);

    xSemaphoreGive(semaphore);
}

void ServerTaskExecuter::Run()
{
    while (true)
    {
        if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
        {
            vTaskDelay(pdMS_TO_TICKS(25));
            continue;
        }

        if (server_messages.empty())
        {
            xSemaphoreGive(semaphore);
            vTaskDelay(pdMS_TO_TICKS(25));
            continue;
        }

        TcpMessage *message = server_messages.front();
        server_messages.pop();
        xSemaphoreGive(semaphore);

        switch (message->GetType())
        {
        case TcpMessageType::SetupClientWithServer:
            ProcessSetupClientWithServer(message);
            break;
        case TcpMessageType::Authentication:
            ProcessAuthenticationMessage(message);
            break;
        case TcpMessageType::AudioData:
            ProcessAudioDataMessage(message);
            break;
        case TcpMessageType::StopSendingAudioData:
            ProcessStopSendingAudioDataMessage(message);
            break;
        case TcpMessageType::Action:
            ProcessActionMessage(message);
            break;
        case TcpMessageType::TransferAudioDevices:
            ProcessTransferAudioDevicesMessage(message);
            break;
        case TcpMessageType::UpdateClientSettings:
            ProcessUpdateClientSettingsMessage(message);
            break;
        default:
            throw std::runtime_error("Message type is not implemented");
        }

        // Cleanup data
        if (message->GetType() != TcpMessageType::AudioData) // Audio player will cleanup data itself, so we dont need to allocate the data again for playing in another buffer
            free(message->GetData());                        // use free because of psram
        delete message;
    }
}

void ServerTaskExecuter::ProcessSetupClientWithServer(TcpMessage *message)
{
    DebugPrintln("Setup client with server");

    int64_t data_length = message->GetDataLength();
    ServerConnectionTask::access_token = new byte[data_length];
    std::memcpy(ServerConnectionTask::access_token, message->GetData(), data_length);

    Settings::SetAccessToken(ServerConnectionTask::access_token, data_length);
    Settings::SetServerAddress(ServerConnectionTask::host);
    Settings::SetServerCertificate(ServerConnectionTask::server_certificate.c_str(), ServerConnectionTask::server_certificate.length());
    Settings::SetIsConfigured(true);

    guid_t event_id;
    message->GetEventId(event_id);
    byte response[1];
    response[0] = 4; // Client type micro controller
    TcpMessage response_message(TcpMessageType::SetupClientWithServer, event_id, response, 1);
    ServerConnectionTask::instance->SendMessageToServer(&response_message);

    DebugPrintln("Reboot esp...");
    ESP.restart();
}

void ServerTaskExecuter::ProcessAuthenticationMessage(TcpMessage *message)
{
    // not allowed, the client can not be configured manually
}

void ServerTaskExecuter::ProcessAudioDataMessage(TcpMessage *message)
{
    DebugPrintln("Play audio from server");
    AudioPlayerTask::instance->Play(message->GetData(), message->GetDataLength(), true);
}

void ServerTaskExecuter::ProcessStopSendingAudioDataMessage(TcpMessage *message)
{
    DebugPrintln("Stop sending audio to server");
    WakeWordDetectionTask::instance->StopAudioStreamToServer();
}

void ServerTaskExecuter::ProcessTransferAudioDevicesMessage(TcpMessage *message)
{
    DebugPrintln("Process transfer aduio devices to server");

    guid_t event_id;
    message->GetEventId(event_id);
    String response = "{\"OutputDevices\":[{\"Item1\":null,\"Item2\":\"System\"}],\"InputDevices\":[{\"Item1\":null,\"Item2\":\"System\"}]}";
    TcpMessage response_message(TcpMessageType::TransferAudioDevices, event_id, (byte *)response.begin(), response.length());
    ServerConnectionTask::instance->SendMessageToServer(&response_message);
}

void ServerTaskExecuter::ProcessUpdateClientSettingsMessage(TcpMessage *message)
{
    DebugPrintln("Update client settings");

    byte update_success = 1;
    DeserializationError error = deserializeJson(json_document, message->GetData());
    if (error == DeserializationError::Ok)
    {
        bool play_request_sound = json_document["PlayRequestSound"];
        DebugPrintf("Set play request sound to: %d\n", (int)play_request_sound);
        Settings::SetPlayRequestSound(play_request_sound);
        WakeWordDetectionTask::instance->SetPlayRequestSound(play_request_sound);

        float volume = json_document["OutputAudioVolume"];
        DebugPrintf("Set volume to: %.2f\n", volume);
        Settings::SetVolume(volume);
        AudioPlayerTask::instance->SetVolume(volume);
    }
    else
    {
        update_success = 0;
        DebugPrintf("Error by deserialize client settings: %s\n", error.c_str());
    }

    guid_t event_id;
    message->GetEventId(event_id);
    byte success[1];
    success[0] = update_success;
    TcpMessage response_message(TcpMessageType::UpdateClientSettings, event_id, success, 1);
    ServerConnectionTask::instance->SendMessageToServer(&response_message);
}

void ServerTaskExecuter::ProcessActionMessage(TcpMessage *message)
{
    DebugPrintln("Process action message from server");
    DeserializationError error = deserializeJson(json_document, message->GetData());
    if (error != DeserializationError::Ok)
    {
        DebugPrintf("Error by deserialize action message: %s\n", error.c_str());
        return;
    }

    TcpMessageActionType type = static_cast<TcpMessageActionType>(json_document["Type"].as<int>());
    switch (type)
    {
    case TcpMessageActionType::SystemAction:
        ProcessSystemAction();
        break;
    case TcpMessageActionType::MusicAction:
        // currently not supported
        break;
    case TcpMessageActionType::TimerAction:
        ProcessTimerAction();
        break;
    }

    guid_t event_id;
    message->GetEventId(event_id);
    byte response[0];
    TcpMessage response_message(TcpMessageType::Action, event_id, response, 0);
    ServerConnectionTask::instance->SendMessageToServer(&response_message);
}

void ServerTaskExecuter::ProcessSystemAction()
{
    if (!json_document["IncreaseVolume"].isNull())
    {
        DebugPrintln("Increase Volume");

        float volume = std::min((float)1, (float)(Settings::GetVolume() + 0.1));
        Settings::SetVolume(volume);
        AudioPlayerTask::instance->SetVolume(volume);
    }

    if (!json_document["DecreaseVolume"].isNull())
    {
        DebugPrintln("Decrease Volume");

        float volume = std::max((float)0, (float)(Settings::GetVolume() - 0.1));
        Settings::SetVolume(volume);
        AudioPlayerTask::instance->SetVolume(volume);
    }

    if (!json_document["SetVolume"].isNull())
    {
        float volume = json_document["SetVolume"];
        DebugPrintf("Set Volume %.2f\n", volume);
        volume = std::max((float)0, std::min((float)1, volume));
        Settings::SetVolume(volume);
        AudioPlayerTask::instance->SetVolume(volume);
    }
}

void ServerTaskExecuter::ProcessTimerAction()
{
    if (!json_document["SetTimer"].isNull())
    {
        long duration = json_document["Duration"];
        DebugPrintf("Set Timer for %ld ms\n", duration);

        if (xTimerIsTimerActive(timer_handle) == pdTRUE)
            xTimerStop(timer_handle, 100);

        xTimerChangePeriod(timer_handle, pdMS_TO_TICKS(duration), 100);
        xTimerStart(timer_handle, 100);
    }

    if (!json_document["DeleteTimer"].isNull())
    {
        DebugPrintln("Delete Timer");

        if (xTimerIsTimerActive(timer_handle) == pdTRUE)
            xTimerStop(timer_handle, 100);
    }
}

void ServerTaskExecuter::PlayTimerSound(TimerHandle_t timer_handle)
{
    for (size_t i = 0; i < 10; i++)
    {
        AudioPlayerTask::instance->Play(request_sound, request_sound_len, false); // use request size also for timer, as no more data fit on the esp
        vTaskDelay(pdMS_TO_TICKS(1000));
    }
}