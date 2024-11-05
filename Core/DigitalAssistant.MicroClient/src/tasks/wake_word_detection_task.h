#pragma once
#include "modules/debug_printer.h"
#include "config.h"
#include "modules/audio/audio_ring_buffer_reader.h"
#include "modules/wake_word_model/wake_word_model.h"
#include "modules/audio/audio_spectrogram.h"
#include "tasks/audio_recorder_task.h"
#include "modules/server_connection/tcp_message.h"
#include "modules/sound_effects/request_sound.h"
#include <algorithm>

class WakeWordDetectionTask
{
public:
  static void Start(void *pvParameter);
  static WakeWordDetectionTask *instance;

  void StopAudioStreamToServer();
  void SetPlayRequestSound(bool new_play_request_sound);
  
private:
  void Setup();
  void Run();

  WakeWordModel wake_word_model;
  AudioRecorderTask *audio_recorder_task;
  AudioSpectrogram audio_spectrogram;

  uint samples_available = 0;
  uint last_buffer_position = 0;
  ulong calculation_time_sum = 0;
  int no_of_caluclations = 0;
  float prediction_result = 0;
  ulong start = 0;
  ulong end = 0;

  bool wake_word_detected = false;
  ulong audio_data_streamed = 0;
  guid_t current_audio_event_id;
  int16_t audio_transfer_buffer[AUDIO_TRANSFER_BUFFER_SIZE];

  bool play_request_sound = true;
};