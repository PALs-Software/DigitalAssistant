#pragma once
#include <Arduino.h>

#define EnableDebug //Comment out if no debug

#ifdef EnableDebug

#define DebugPrint(x) Serial.print(x)
#define DebugPrintln(x) Serial.println(x)
#define DebugPrintf(...) Serial.printf(__VA_ARGS__)

#else

#define DebugPrint(x)
#define DebugPrintln(x)
#define DebugPrintf(...)

#endif
