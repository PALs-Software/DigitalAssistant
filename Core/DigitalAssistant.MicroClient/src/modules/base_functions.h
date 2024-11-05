#pragma once

class BaseFunctions
{
public:
    static uint32_t GetFreeRam()
    {
       return ESP.getFreeHeap();
    }

    static bool StringIsNullOrEmpty(String s)
    {
        if (s == NULL || s.length() == 0)
            return true;
        return false;
    }
};
