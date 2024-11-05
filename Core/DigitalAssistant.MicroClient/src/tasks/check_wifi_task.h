#pragma once
#include <Wifi.h>
#include "../modules/debug_printer.h"
#include "../config.h"

class CheckWifiTask
{
public:
      static void Start(void *pvParameter);

private:
      void Setup();
      void Run();
};
