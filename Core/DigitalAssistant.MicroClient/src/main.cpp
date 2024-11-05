#include <Arduino.h>
#include "modules/settings.h"
#include "tasks/audio_player_task.h"
#include "tasks/audio_recorder_task.h"
#include "tasks/check_wifi_task.h"
#include "tasks/debug_print_task.h"
#include "tasks/server_connection_task.h"
#include "tasks/server_task_executer.h"
#include "tasks/wake_word_detection_task.h"

Preferences storage;
AudioPlayerTask audio_player_task;
AudioRecorderTask audio_recorder_task;
CheckWifiTask check_wifi_task;

ServerConnectionTask server_connection_task;
ServerTaskExecuter server_task_executer;
WakeWordDetectionTask wake_word_detection_task;

#if defined(EnableDebug)
DebugPrintTask debug_print_task;
#endif

void setup()
{
  Settings::Init(&storage);

#if defined(EnableDebug)
  Serial.begin(115200);
  DebugPrintln("Debugging is On!");
  xTaskCreate(DebugPrintTask::Start, "Debug Print Task", 4096, &debug_print_task, 1, NULL);
#endif

  xTaskCreate(CheckWifiTask::Start, "Check Wifi Task", 4096, &check_wifi_task, 1, NULL);
  xTaskCreate(ServerConnectionTask::Start, "Server Connection Task", 4096, new void *[2]{&server_connection_task, &server_task_executer}, 1, NULL);
  xTaskCreate(ServerTaskExecuter::Start, "Server Task Executer", 8192, &server_task_executer, 1, NULL);

  if (Settings::GetIsConfigured())
  {
    xTaskCreate(AudioPlayerTask::Start, "Audio Player Task", 4096, &audio_player_task, 1, NULL);
    xTaskCreate(AudioRecorderTask::Start, "Audio Recorder Task", 4096, &audio_recorder_task, 1, NULL);
    xTaskCreate(WakeWordDetectionTask::Start, "Wake Word Detection Task", 4096, new void *[2]{&wake_word_detection_task, &audio_recorder_task}, 1, NULL);
  }
}

void loop()
{
}