#pragma once
#include "../modules/debug_printer.h"
#include "../config.h"
#include "modules/audio/audio_ring_buffer_reader.h"
#include <driver/i2s.h>

class AudioRecorderTask
{
public:
  static void Start(void *pvParameter);

  uint NoOfNewSamplesAvailable();
  int16_t* GetAudioBuffer();
  uint GetCurrentBufferPosition();

private:
  void Setup();
  void Run();

  AudioRingBufferReader *audio_ring_buffer_reader;
  int16_t *audio_buffer;
  int16_t i2s_data_buffer[I2S_AUDIO_RECORDER_BUFFER_SIZE];
  uint no_of_samples_read;
  SemaphoreHandle_t semaphore = xSemaphoreCreateMutex();
  QueueHandle_t i2s_queue;
};
