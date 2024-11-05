#include "wake_word_detection_task.h"
#include "server_connection_task.h"
#include "audio_player_task.h"

WakeWordDetectionTask *WakeWordDetectionTask::instance;

void WakeWordDetectionTask::Start(void *pvParameters)
{
   void **parameters = (void **)pvParameters;
   WakeWordDetectionTask *task = (WakeWordDetectionTask *)parameters[0];
   task->audio_recorder_task = (AudioRecorderTask *)parameters[1];

   instance = task;
   task->Setup();
   task->Run();
}

void WakeWordDetectionTask::Setup()
{
   DebugPrintln("Setup wake word detection task");
   audio_spectrogram.Init();
}

void WakeWordDetectionTask::Run()
{
   while (true)
   {
      samples_available += audio_recorder_task->NoOfNewSamplesAvailable();

      if (wake_word_detected)
      {
         if (samples_available < SAMPLE_RATE / 4)
         {
            vTaskDelay(pdMS_TO_TICKS(50));
            continue;
         }

         if (audio_data_streamed > MAX_AUDIO_STREAM_LENGTH)
         {
            StopAudioStreamToServer();
            continue;
         }

         AudioRingBufferReader send_buffer_reader(audio_recorder_task->GetAudioBuffer());

         send_buffer_reader.SetPosition(last_buffer_position);
         uint samples_to_transfer = std::min(samples_available, (uint)AUDIO_TRANSFER_BUFFER_SIZE);
         for (int i = 0; i < samples_to_transfer; i++)
         {
            audio_transfer_buffer[i] = send_buffer_reader.GetCurrentSample();
            send_buffer_reader.MoveForward();
         }
         last_buffer_position = send_buffer_reader.GetPosition();
         samples_available -= samples_to_transfer;
         audio_data_streamed += samples_to_transfer;

         TcpMessage response_message(TcpMessageType::AudioData, current_audio_event_id, (byte *)audio_transfer_buffer, samples_to_transfer * sizeof(int16_t));
         ServerConnectionTask::instance->SendMessageToServer(&response_message);
         continue;
      }

      if (samples_available < WAKE_WORD_WIDTH)
      {
         vTaskDelay(pdMS_TO_TICKS(50));
         continue;
      }

      AudioRingBufferReader buffer_reader(audio_recorder_task->GetAudioBuffer());

      // As the calculation cannot take place in real time, we have to put some data in the bin
      if (samples_available > WAKE_WORD_MAX_BUFFER_DELAY_LENGTH)
      {
         buffer_reader.SetPosition(audio_recorder_task->GetCurrentBufferPosition() - WAKE_WORD_WIDTH);
         last_buffer_position = buffer_reader.GetPosition();
         samples_available = WAKE_WORD_WIDTH;
         DebugPrintln("Removed samples from audio buffer for the wake word detection because calculation cannot take place in real time");
      }
      else
      {
         // Step one wake word step width forward
         buffer_reader.SetPosition(last_buffer_position + WAKE_WORD_STEP_WIDTH);
         last_buffer_position = buffer_reader.GetPosition();
         samples_available -= WAKE_WORD_STEP_WIDTH;
      }

      start = millis();
      audio_spectrogram.GetSpectrogram(&buffer_reader, wake_word_model.GetInputBuffer());
      prediction_result = wake_word_model.Predict();
      end = millis();

      calculation_time_sum += end - start;
      no_of_caluclations++;

      if (no_of_caluclations > 10)
      {
         DebugPrintf("Average calculation time for the wakeword: %.2f ms\n", calculation_time_sum / (float)no_of_caluclations);
         no_of_caluclations = 0;
         calculation_time_sum = 0;
      }

      DebugPrintf("Prediction result: %.2f\n", prediction_result);

      if (prediction_result > WAKE_WORD_CONFIDENCE_LEVEL)
      {
         DebugPrintf("Wake word detected: %.2f %\n", prediction_result);
         wake_word_detected = true;
         create_guid(current_audio_event_id);

         if (play_request_sound){
            float current_volume = AudioPlayerTask::instance->GetVolume();
            AudioPlayerTask::instance->SetVolume(0.1);
            AudioPlayerTask::instance->Play(request_sound, request_sound_len, false);
            AudioPlayerTask::instance->SetVolume(current_volume);
         }
            
      }
   }
}

void WakeWordDetectionTask::StopAudioStreamToServer()
{
   DebugPrintln("Stop audio streaming to the server");
   wake_word_detected = false;
}

void WakeWordDetectionTask::SetPlayRequestSound(bool new_play_request_sound)
{
   play_request_sound = new_play_request_sound;
}