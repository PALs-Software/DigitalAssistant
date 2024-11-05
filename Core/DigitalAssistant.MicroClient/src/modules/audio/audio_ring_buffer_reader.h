#pragma once
#include "config.h"
#include <string.h>

// Audio Ring Buffer Reader of a simple audio buffer array in a loop
// Do everything inline to increase performance
class AudioRingBufferReader
{
public:
    AudioRingBufferReader(int16_t *audio_buffer)
    {
        buffer = audio_buffer;
        position = 0;
    }

    // Get current position in the loop
    inline uint GetPosition()
    {
        return position;
    }

    // Set current position in the loop, also negative positions are possible to start over at the end of the buffer array
    inline void SetPosition(uint new_position)
    {
        // if position negative -> start over at the end of the array
        position = (new_position + AUDIO_BUFFER_SIZE) % AUDIO_BUFFER_SIZE;
    }

    // Get sample at current position
    inline int16_t GetCurrentSample()
    {
        return buffer[position];
    }

    // Set sample at current position
    inline void SetCurrentSample(int16_t sample)
    {
        buffer[position] = sample;
    }

    // Move one sample forward in the buffer array and if we hit the end, start over from the beginning
    inline void MoveForward()
    {
        position++;
        if (position == AUDIO_BUFFER_SIZE)
            position = 0;
    }

private:
    int16_t *buffer;
    uint position = 0;
};