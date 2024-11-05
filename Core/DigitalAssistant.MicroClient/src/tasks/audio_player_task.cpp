#include "audio_player_task.h"
#include <cstring>

AudioPlayerTask *AudioPlayerTask::instance;

void AudioPlayerTask::Start(void *pvParameters)
{
    AudioPlayerTask *task = (AudioPlayerTask *)pvParameters;
    instance = task;
    task->Setup();
    task->Run();
}

void AudioPlayerTask::Setup()
{
    DebugPrintln("Setup audio player task");

    volume = Settings::GetVolume();

    i2s_config_t i2s_config = {
        .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX),
        .sample_rate = SAMPLE_RATE,
        .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT,
        .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
        .communication_format = I2S_COMM_FORMAT_STAND_I2S,
        .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
        .dma_buf_count = 8,
        .dma_buf_len = 512,
        .use_apll = false,
        .tx_desc_auto_clear = false,
        .fixed_mclk = 0};

    i2s_pin_config_t i2s_pin_config = {
        .bck_io_num = I2S_SPEAKER_SERIAL_CLOCK_PIN,
        .ws_io_num = I2S_SPEAKER_LEFT_RIGHT_CLOCK_PIN,
        .data_out_num = I2S_SPEAKER_SERIAL_DATA_PIN,
        .data_in_num = I2S_PIN_NO_CHANGE};

    i2s_driver_install(I2S_NUM_1, &i2s_config, 0, nullptr);
    i2s_set_pin(I2S_NUM_1, &i2s_pin_config);
    i2s_zero_dma_buffer(I2S_NUM_1);
}

void AudioPlayerTask::Run()
{
    while (true)
    {
        if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
        {
            vTaskDelay(pdMS_TO_TICKS(50));
            continue;
        }

        if (data_arrays_to_play.empty())
        {
            xSemaphoreGive(semaphore);
            vTaskDelay(pdMS_TO_TICKS(50));
            continue;
        }

        DebugPrintln("Play new audio");

        std::tuple<byte *, size_t, bool> data = data_arrays_to_play.front();
        byte *original_bytes_to_write_pointer = std::get<0>(data);
        size_t no_of_bytes_to_write = std::get<1>(data);
        bool free_pointer = std::get<2>(data);
        data_arrays_to_play.pop();
        xSemaphoreGive(semaphore);

        byte *sample_copy = (byte *)ps_malloc(no_of_bytes_to_write);
        std::memcpy(sample_copy, original_bytes_to_write_pointer, no_of_bytes_to_write);

       int16_t *int16_pointer = (int16_t *)sample_copy;
        for (size_t i; i < no_of_bytes_to_write / sizeof(int16_t); i++)
            int16_pointer[i] *= volume;

        i2s_zero_dma_buffer(I2S_NUM_1);
        i2s_start(I2S_NUM_1);

        byte *bytes_to_write = (byte *)sample_copy;
        size_t bytes_written = 0;
        do
        {
            esp_err_t result = i2s_write(I2S_NUM_1, bytes_to_write, no_of_bytes_to_write, &bytes_written, 100);
            if (result != ESP_OK)
                break;

            no_of_bytes_to_write -= bytes_written;
            bytes_to_write += bytes_written;

        } while (bytes_written > 0 && no_of_bytes_to_write > 0);

        vTaskDelay(pdMS_TO_TICKS(100));
        i2s_stop(I2S_NUM_1);

        free(sample_copy);
        if (free_pointer)
            free(original_bytes_to_write_pointer);
    }
}

void AudioPlayerTask::Play(byte *data, size_t number_of_bytes_to_play, bool free_pointer)
{
    if (xSemaphoreTake(semaphore, portMAX_DELAY) != pdTRUE)
        return;

    data_arrays_to_play.push(std::make_tuple(data, number_of_bytes_to_play, free_pointer));
    xSemaphoreGive(semaphore);
}

void AudioPlayerTask::SetVolume(float new_volume)
{
    volume = new_volume;
}

float AudioPlayerTask::GetVolume()
{
    return volume;
}