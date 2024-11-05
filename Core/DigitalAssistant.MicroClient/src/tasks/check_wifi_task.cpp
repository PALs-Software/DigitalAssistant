#include "check_wifi_task.h"

void CheckWifiTask::Start(void *pvParameter)
{
    CheckWifiTask *task = (CheckWifiTask *)pvParameter;
    task->Setup();
    task->Run();
}

void CheckWifiTask::Setup()
{
    DebugPrintln("Setup Check Wifi Task");
}

void CheckWifiTask::Run()
{
    while (true)
    {
        DebugPrintln("Check wifi is connected");

        if (WiFi.status() == WL_CONNECTED)
        {
            vTaskDelay(pdMS_TO_TICKS(10000));
            continue;
        }

        DebugPrintln("Wifi is currently not connected, start connect...");

        WiFi.mode(WIFI_STA);
        WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
        WiFi.setSleep(false);

        int counter = 0;
        do
        {
            vTaskDelay(pdMS_TO_TICKS(1000));
            DebugPrintln("Try connect wifi... (Status: " + String(WiFi.status()) + ")");
            counter++;

        } while (WiFi.status() != WL_CONNECTED && counter < 120);

        if (WiFi.status() == WL_CONNECTED)
            DebugPrintln("WiFi connected (IP address: " + WiFi.localIP().toString() + ")");
        else
            DebugPrintln("Try wifi connect again...");
    }
}