#pragma once

#include "config.h"
#include "tools/kiss_fftr.h"
#include "modules/audio/audio_ring_buffer_reader.h"
#include <algorithm>
#include "modules/debug_printer.h"

class AudioSpectrogram
{
public:
    void Init();
    void GetSpectrogram(AudioRingBufferReader *ring_buffer_reader, float *spectrogram);

private:
    int fft_size;
    int fft_height;
    int pooled_fft_height;

    float *hann_window;
    float *fft_input;
    float *fft_column;
    kiss_fftr_cfg fft_cfg;
    kiss_fft_cpx *fft_output;
    const float EPSILON = 1e-6f;

    void CalculateFftWindow(float *output);
    float *CreateHannPeriodicWindow(int width);

    float GetMean(AudioRingBufferReader *ring_buffer_reader);
    float GetMax(AudioRingBufferReader *ring_buffer_reader, float mean);
};
