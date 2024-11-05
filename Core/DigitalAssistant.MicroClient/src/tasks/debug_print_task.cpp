#include "debug_print_task.h"

void DebugPrintTask::Start(void *pvParameter)
{
   DebugPrintTask *task = (DebugPrintTask *)pvParameter;
   task->Setup();
   task->Run();
}

void DebugPrintTask::Setup()
{
   DebugPrintln("Setup Debug Print Task");
}

void DebugPrintTask::Run()
{
   while (true)
   {
      DebugPrintf("Still running... (Total Heap: %d, Free Heap: %d, Total PSRAM: %d, Free PSRAM: %d)\n", ESP.getHeapSize(), ESP.getFreeHeap(), ESP.getPsramSize(), ESP.getFreePsram());
      vTaskDelay(pdMS_TO_TICKS(5000));
   }
}