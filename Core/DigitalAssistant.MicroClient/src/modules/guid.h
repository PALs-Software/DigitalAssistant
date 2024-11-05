// Changed version of esp32-uuid library https://github.com/typester/esp32-uuid/tree/master

#pragma once

#include <Arduino.h>
#include "esp_system.h"

typedef uint8_t guid_t[16];

void create_guid(guid_t guid);
String convert_guid_to_string(guid_t guid);