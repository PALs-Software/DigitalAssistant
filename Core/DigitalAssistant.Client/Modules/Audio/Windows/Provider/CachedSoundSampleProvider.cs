using NAudio.Wave;

namespace DigitalAssistant.Client.Modules.Audio.Windows.Provider;

public class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
{
    public WaveFormat WaveFormat { get { return CachedSound.WaveFormat; } }

    protected readonly CachedSound CachedSound = cachedSound;
    protected long Position;

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = CachedSound.AudioData.Length - Position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(CachedSound.AudioData, Position, buffer, offset, samplesToCopy);
        Position += samplesToCopy;
        return (int)samplesToCopy;
    }
}
