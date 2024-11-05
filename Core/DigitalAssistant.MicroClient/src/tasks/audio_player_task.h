#pragma once
#include "../modules/debug_printer.h"
#include "../config.h"
#include "modules/audio/audio_ring_buffer_reader.h"
#include <driver/i2s.h>
#include <queue>
#include "modules/settings.h"

class AudioPlayerTask
{
public:
  static void Start(void *pvParameter);
  static AudioPlayerTask *instance;

  void Play(byte *data, size_t number_of_bytes_to_play, bool free_pointer);

  void SetVolume(float new_volume);
  float GetVolume();

private:
  void Setup();
  void Run();

  std::queue<std::tuple<byte *, size_t, bool>> data_arrays_to_play;
  SemaphoreHandle_t semaphore = xSemaphoreCreateMutex();
  int16_t i2s_data_buffer[I2S_AUDIO_PLAYER_BUFFER_SIZE];
  float volume = 0.5;
};