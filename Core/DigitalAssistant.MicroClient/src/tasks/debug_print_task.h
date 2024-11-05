#pragma once
#include "../modules/debug_printer.h"
#include "../modules/base_functions.h"

class DebugPrintTask
{
public:
       static void Start(void *pvParameter);

private:
       void Setup();
       void Run();
};
