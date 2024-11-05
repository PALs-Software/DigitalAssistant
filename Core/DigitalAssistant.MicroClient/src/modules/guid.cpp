// Changed version of esp32-uuid library https://github.com/typester/esp32-uuid/tree/master

#include "guid.h"

void create_guid(guid_t guid)
{
    esp_fill_random(guid, sizeof(guid_t));
    guid[6] = 0x40 | (guid[6] & 0xF);
    guid[8] = (0x80 | guid[8]) & ~0x40;
}

String convert_guid_to_string(guid_t guid)
{
    char result[37];
    snprintf(result, 37,
             "%02x%02x%02x%02x-%02x%02x-%02x%02x-"
             "%02x%02x-%02x%02x%02x%02x%02x%02x",
             guid[0], guid[1], guid[2], guid[3], guid[4], guid[5], guid[6], guid[7], guid[8], guid[9], guid[10], guid[11],
             guid[12], guid[13], guid[14], guid[15]);

    return String(result);
}