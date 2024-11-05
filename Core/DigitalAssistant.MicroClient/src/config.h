#pragma once

#define WIFI_SSID "WIFI_SSID"
#define WIFI_PASSWORD "WIFI_PASSWORD"

// General audio config
#define SAMPLE_RATE 16000 // Sample rate
#define AUDIO_BUFFER_SIZE (SAMPLE_RATE * 10) // Total audio buffer size.

// Audio recorder config
#define I2S_AUDIO_RECORDER_BUFFER_SIZE 1024 // Buffer size for the i2s interface for the microphone
#define I2S_MICROPHONE_SERIAL_CLOCK_PIN GPIO_NUM_5 // SCK
#define I2S_MICROPHONE_LEFT_RIGHT_CLOCK_PIN GPIO_NUM_6 // WS
#define I2S_MICROPHONE_SERIAL_DATA_PIN GPIO_NUM_4 //SD

// Audio player config
#define I2S_AUDIO_PLAYER_BUFFER_SIZE 1024 // Buffer size for the i2s interface for the speaker
#define I2S_SPEAKER_SERIAL_CLOCK_PIN GPIO_NUM_13 // SCK
#define I2S_SPEAKER_LEFT_RIGHT_CLOCK_PIN GPIO_NUM_14 // WS
#define I2S_SPEAKER_SERIAL_DATA_PIN  GPIO_NUM_12 //SD

// Wake word detection config
#define WAKE_WORD_WIDTH 32000 // No of samples for one detection batch
#define WAKE_WORD_STEP_WIDTH (SAMPLE_RATE / 1000 * 600) // Step width for the detection window -> 600ms
#define WAKE_WORD_MAX_BUFFER_DELAY_LENGTH (WAKE_WORD_WIDTH * 3) // Max allowed delay the wake word detection is allowed to hang behind if it can not keep up in real time
#define WAKE_WORD_CONFIDENCE_LEVEL 0.90 // Wake word confidence level
#define MAX_AUDIO_STREAM_LENGTH (SAMPLE_RATE * 10) // 10 seconds
#define TENSOR_ARENA_SIZE 45000

// Spectrogram calculation config
#define SPECTROGRAM_WINDOW_SIZE 320 // Window width for the fft calculation of the spectrogram
#define SPECTROGRAM_STEP_SIZE 160 // Window step size for the fft calculation of the spectrogram
#define SPECTROGRAM_POOLING_SIZE 6 // Pooling size for the fft calculation of one column of the spectrogram

// Server connection config
#define CONNECTION_BUFFER_SIZE 131072 // Used connection buffer size for reading data from server -> 128 kb in default
#define AUDIO_TRANSFER_BUFFER_SIZE (SAMPLE_RATE / 2) // Used audio transfer buffer size in samples -> 16 kb in default