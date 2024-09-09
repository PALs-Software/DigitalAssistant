using NAudio.Wave;

namespace DigitalAssistant.Client.Modules.Audio.Windows.Provider;

class ArraySampleProvider(float[] audioData, WaveFormat waveFormat) : ISampleProvider
{
    #region Properties
    public float[] AudioData { get; init; } = audioData;
    public WaveFormat WaveFormat { get; init; } = waveFormat;
    public int Position { get; protected set; } = 0;
    #endregion

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = AudioData.Length - Position;
        var samplesToCopy = Math.Min(availableSamples, count);

        Array.Copy(AudioData, Position, buffer, offset, samplesToCopy);

        Position += samplesToCopy;
        return samplesToCopy;
    }
}