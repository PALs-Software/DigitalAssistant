#include "audio_spectrogram.h"

void AudioSpectrogram::Init()
{
    fft_size = 1;
    while (fft_size < SPECTROGRAM_WINDOW_SIZE) // fft size needs to be power of 2, for good performance
        fft_size <<= 1;

    fft_height = fft_size / 2 + 1;
    pooled_fft_height = ceilf((float)fft_height / (float)SPECTROGRAM_POOLING_SIZE);

    fft_input = new float[fft_size];
    fft_output = static_cast<kiss_fft_cpx *>(malloc(sizeof(kiss_fft_cpx) * fft_height));
    fft_column = new float[fft_height];
    fft_cfg = kiss_fftr_alloc(fft_size, false, 0, 0);
    hann_window = CreateHannPeriodicWindow(SPECTROGRAM_WINDOW_SIZE);

    DebugPrintf("FFTSize: %d, FFTHeight: %d, PooledFFTHeight: %d\n", fft_size, fft_height, pooled_fft_height);
}

void AudioSpectrogram::GetSpectrogram(AudioRingBufferReader *ring_buffer_reader, float *spectrogram)
{
    uint start_position = ring_buffer_reader->GetPosition();
    float mean = GetMean(ring_buffer_reader);
    float max = GetMax(ring_buffer_reader, mean);

    int ffts_to_process = (int)((WAKE_WORD_WIDTH - SPECTROGRAM_WINDOW_SIZE) / (float)SPECTROGRAM_STEP_SIZE + 1);
    for (int i = 0; i < ffts_to_process; i++)
    {
        ring_buffer_reader->SetPosition(start_position + i * SPECTROGRAM_STEP_SIZE);

        for (int y = 0; y < SPECTROGRAM_WINDOW_SIZE; y++)
        {
            fft_input[y] = ((float)ring_buffer_reader->GetCurrentSample() - mean) / max;
            ring_buffer_reader->MoveForward();
        }

        for (int y = SPECTROGRAM_WINDOW_SIZE; y < fft_size; y++)
            fft_input[y] = 0;

        // Apply hanning window
        for (int y = 0; y < SPECTROGRAM_WINDOW_SIZE; y++)
            fft_input[y] = fft_input[y] * hann_window[y];

        CalculateFftWindow(spectrogram);
        spectrogram += pooled_fft_height;
    }
}

void AudioSpectrogram::CalculateFftWindow(float *output)
{
    kiss_fftr(fft_cfg, fft_input, reinterpret_cast<kiss_fft_cpx *>(fft_output));

    // Calculate Magnitude
    for (int i = 0; i < fft_height; i++)
        fft_column[i] = (fft_output[i].r * fft_output[i].r) + (fft_output[i].i * fft_output[i].i);

    // Pool the array to reduce the size with averaging the values
    int operating_index = 0;
    for (int i = 0; i < fft_height; i += SPECTROGRAM_POOLING_SIZE)
    {
        float averageValue = 0;
        for (int j = 0; j < SPECTROGRAM_POOLING_SIZE; j++)
            if (i + j < fft_height)
                averageValue += fft_column[i + j];

        fft_column[operating_index] = averageValue / SPECTROGRAM_POOLING_SIZE;
        operating_index++;
    }

    for (int i = 0; i < pooled_fft_height; i++)
        output[i] = log10f(fft_column[i] + EPSILON);
}

float *AudioSpectrogram::CreateHannPeriodicWindow(int width)
{
    float num = M_PI * 2.0 / width;
    float *array = new float[width];
    for (int i = 0; i < width; i++)
        array[i] = 0.5 - 0.5 * cos(i * num);

    return array;
}

float AudioSpectrogram::GetMean(AudioRingBufferReader *ring_buffer_reader)
{
    uint position = ring_buffer_reader->GetPosition();

    float mean = 0;
    for (int i = 0; i < WAKE_WORD_WIDTH; i++)
    {
        mean += ring_buffer_reader->GetCurrentSample();
        ring_buffer_reader->MoveForward();
    }
    mean /= WAKE_WORD_WIDTH;

    ring_buffer_reader->SetPosition(position);
    return mean;
}

float AudioSpectrogram::GetMax(AudioRingBufferReader *ring_buffer_reader, float mean)
{
    uint position = ring_buffer_reader->GetPosition();

    float max = 0;
    for (int i = 0; i < WAKE_WORD_WIDTH; i++)
    {
        max = std::max(max, fabsf(((float)ring_buffer_reader->GetCurrentSample()) - mean));
        ring_buffer_reader->MoveForward();
    }

    ring_buffer_reader->SetPosition(position);
    return max;
}