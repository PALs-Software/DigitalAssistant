#include "audio_recorder_task.h"

void AudioRecorderTask::Start(void *pvParameters)
{
   AudioRecorderTask *task = (AudioRecorderTask *)pvParameters;
   task->Setup();
   task->Run();
}

void AudioRecorderTask::Setup()
{
   DebugPrintln("Setup audio recorder task");

   audio_buffer = (int16_t *)ps_malloc(AUDIO_BUFFER_SIZE * sizeof(int16_t));
   memset(audio_buffer, 0, AUDIO_BUFFER_SIZE * sizeof(int16_t));

   audio_ring_buffer_reader = new AudioRingBufferReader(audio_buffer);

   i2s_config_t i2s_config = {
       .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX),
       .sample_rate = SAMPLE_RATE,
       .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
       .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
       .communication_format = I2S_COMM_FORMAT_STAND_I2S,
       .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
       .dma_buf_count = 8,
       .dma_buf_len = I2S_AUDIO_RECORDER_BUFFER_SIZE, // is in samples not bytes
       .use_apll = false,
       .tx_desc_auto_clear = false,
       .fixed_mclk = 0};

   i2s_pin_config_t i2s_pin_config = {
       .bck_io_num = I2S_MICROPHONE_SERIAL_CLOCK_PIN,
       .ws_io_num = I2S_MICROPHONE_LEFT_RIGHT_CLOCK_PIN,
       .data_out_num = I2S_PIN_NO_CHANGE,
       .data_in_num = I2S_MICROPHONE_SERIAL_DATA_PIN};

   i2s_driver_install(I2S_NUM_0, &i2s_config, 4, &i2s_queue);
   i2s_set_pin(I2S_NUM_0, &i2s_pin_config);
}

void AudioRecorderTask::Run()
{
   while (true)
   {
      i2s_event_t i2s_event;
      if (!xQueueReceive(i2s_queue, &i2s_event, portMAX_DELAY) == pdPASS)
         continue;

      if (i2s_event.type != I2S_EVENT_RX_DONE)
         continue;

      size_t bytes_read = 0;
      do
      {
         esp_err_t result = i2s_read(I2S_NUM_0, i2s_data_buffer, I2S_AUDIO_RECORDER_BUFFER_SIZE * sizeof(int16_t), &bytes_read, 10);
         if (result != ESP_OK || bytes_read == 0)
            break;

         for (int i = 0; i < bytes_read / sizeof(int16_t); i++)
         {
            audio_ring_buffer_reader->SetCurrentSample((i2s_data_buffer[i]));
            audio_ring_buffer_reader->MoveForward();
         }

         if (xSemaphoreTake(semaphore, portMAX_DELAY) == pdTRUE)
         {
            no_of_samples_read += bytes_read / sizeof(int16_t);
            xSemaphoreGive(semaphore);
         }
      } while (bytes_read > 0);
   }
}

uint AudioRecorderTask::NoOfNewSamplesAvailable()
{
   uint result;
   if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
      return 0;

   result = no_of_samples_read;
   no_of_samples_read = 0;
   xSemaphoreGive(semaphore);

   return result;
}

int16_t *AudioRecorderTask::GetAudioBuffer()
{
   return audio_buffer;
}

uint AudioRecorderTask::GetCurrentBufferPosition()
{
   return audio_ring_buffer_reader->GetPosition();
}