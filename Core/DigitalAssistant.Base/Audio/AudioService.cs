namespace DigitalAssistant.Base.Audio;

public class AudioService
{

    /// <summary>
    /// <para>Detects if the speaker has finished speaking.</para>
    /// The threshold parameter sets a minimum RMS level, if the RMS level of the last full batch is below this threshold, the speaker is considered to have finished speaking<br/>
    /// The volumeDropFactor parameter sets the factor by which the RMS level of the last batch has to be lower than first batches, if the end batch is below this factor, the speaker is considered to have finished speaking<br/>
    /// The startDetectionDurationInSeconds parameter sets the number of the first samples which will be used to calculate the rms value of the start which will be compared to the rms level of the last samples of the endDetectionDurationInSeconds intervall
    /// The endDetectionDurationInSeconds parameter sets the number of the last samples which will be used to calculate the rms value of the end which will be compared to the rms level of the first samples of the minDetectionDurationInSeconds intervall. A higher value will allow longer pauses between words, but will also increase the detection delay<br/>
    /// The minDetectionDurationInSeconds parameter sets the minimum duration of the detection, if there are less samples than this value, the method will return false<br/>
    /// The maxDetectionDurationInSeconds parameter sets the maximum duration of the detection, if there are more samples than this value, the method will return true<br/>
    /// </summary>
    /// <param name="samples">The audio data which will be checked</param>
    /// <param name="sampleRate">The sample rate of the audio data</param>
    /// <param name="threshold">The threshold parameter sets a minimum RMS level, if the RMS level of the last full batch is below this threshold, the speaker is considered to have finished speaking</param>
    /// <param name="volumeDropFactor">The volumeDropFactor parameter sets the factor by which the RMS level of the last batch has to be lower than first batches, if the end batch is below this factor, the speaker is considered to have finished speaking</param>
    /// <param name="startDetectionDurationInSeconds">The startDetectionDurationInSeconds parameter sets the number of the first samples which will be used to calculate the rms value of the start which will be compared to the rms level of the last samples of the endDetectionDurationInSeconds intervall</param>
    /// <param name="endDetectionDurationInSeconds"> The endDetectionDurationInSeconds parameter sets the number of the last samples which will be used to calculate the rms value of the end which will be compared to the rms level of the first samples of the minDetectionDurationInSeconds intervall. A higher value will allow longer pauses between words, but will also increase the detection delay</param>
    /// <param name="minDetectionDurationInSeconds">The minDetectionDurationInSeconds parameter sets the minimum duration of the detection, if there are less samples than this value, the method will return false</param>
    /// <param name="maxDetectionDurationInSeconds">The maxDetectionDurationInSeconds parameter sets the maximum duration of the detection, if there are more samples than this value, the method will return true</param>
    /// <returns>True, if the speaker has finished speaking</returns>
    public bool SpeakerFinishedSpeaking(Span<float> samples, int sampleRate, float threshold = 0.009f, float volumeDropFactor = 0.4f, float startDetectionDurationInSeconds = 2, float endDetectionDurationInSeconds = 2, float minDetectionDurationInSeconds = 1, float maxDetectionDurationInSeconds = 10)
    {
        var minDetectionSampleLength = minDetectionDurationInSeconds * sampleRate;
        if (samples.Length < minDetectionSampleLength)
            return false;

        var maxDetectionSampleLength = maxDetectionDurationInSeconds * sampleRate;
        if (samples.Length > maxDetectionSampleLength)
            return true;

        var startRmsLevelBatchSize = (int)(startDetectionDurationInSeconds * sampleRate);
        var startRmsLevelSamples = samples.Slice(0, Math.Min(startRmsLevelBatchSize, samples.Length));
        var startRmsLevel = CalculateRms(startRmsLevelSamples);

        var endBatchSize = (int)(endDetectionDurationInSeconds * sampleRate);
        var endBatchSamples = samples.Slice(Math.Max(samples.Length - endBatchSize, 0), Math.Min(endBatchSize, samples.Length));
        var endRmsLevel = CalculateRms(endBatchSamples);

        return endRmsLevel < threshold || endRmsLevel < startRmsLevel * volumeDropFactor;
    }

    public float CalculateRms(float[] samples)
    {
        return CalculateRms(samples.AsSpan());
    }

    public float CalculateRms(Span<float> audioData)
    {
        double sumOfSquares = 0.0;
        foreach (float sample in audioData)
            sumOfSquares += sample * sample;

        double meanSquare = sumOfSquares / audioData.Length;
        float rms = (float)Math.Sqrt(meanSquare);

        return rms;
    }

    public void NormalizeAudioData(float[] audioSamples)
    {
        NormalizeAudioData(audioSamples.AsSpan());
    }

    public void NormalizeAudioData(Span<float> audioSamples)
    {
        double sum = 0;
        foreach (var sample in audioSamples)
            sum += sample;

        var average = (float)(sum / audioSamples.Length);
        for (int i = 0; i < audioSamples.Length; i++)
            audioSamples[i] -= average;

        float max = 0;
        foreach (var sample in audioSamples)
            if (sample > max)
                max = sample;

        if (max > 0)
            for (int i = 0; i < audioSamples.Length; i++)
                audioSamples[i] /= max;
    }
}
