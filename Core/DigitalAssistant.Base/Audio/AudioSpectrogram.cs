using System.Numerics;

namespace DigitalAssistant.Base.Audio;

public class AudioSpectrogram
{
    #region Members
    protected int FftSize;
    protected int WindowSize;
    protected int StepSize;
    protected int PoolingSize;
    protected int FftHeight;
    protected int PooledFftHeight;

    protected double[] HannWindow;
    #endregion

    #region Consts
    private const float EPSILON = 1e-6f;
    #endregion

    public AudioSpectrogram(int windowSize, int stepSize, int poolingSize)
    {
        WindowSize = windowSize;
        StepSize = stepSize;
        PoolingSize = poolingSize;

        FftSize = 1;
        while (FftSize < WindowSize) // fft size needs to be power of 2, for good performance
            FftSize <<= 1;

        FftHeight = FftSize / 2 + 1;
        PooledFftHeight = (int)Math.Ceiling(FftHeight / (float)PoolingSize);

        HannWindow = CreateHannPeriodicWindow(WindowSize);
    }

    public float[][] GetSpectrogram(float[] audioSamples, bool useHannWindow)
    {
        var fftsToProcess = (int)((audioSamples.Length - WindowSize) / (float)StepSize + 1);
        if (fftsToProcess < 1)
            return [];

        var ffts = new float[fftsToProcess][];
        Parallel.For(0, fftsToProcess, currentFftWindowIndex =>
        {
            int startIndex = currentFftWindowIndex * StepSize;
            var fft = CalculateFftWindow(audioSamples, startIndex, useHannWindow);
            ffts[currentFftWindowIndex] = fft;
        });

        return ffts;
    }

    protected float[] CalculateFftWindow(float[] audioData, int fromIndex, bool useHannWindow)
    {
        var fftBuffer = new Complex[FftSize];
        var newFft = new float[FftHeight];
        var pooledFft = new float[PooledFftHeight];

        // fill buffer with audio data
        if (useHannWindow)
        {
            for (int i = 0; i < WindowSize; i++)
                fftBuffer[i] = new Complex(audioData[i + fromIndex] * HannWindow[i], 0);
        }
        else
        {
            for (int i = 0; i < WindowSize; i++)
                fftBuffer[i] = new Complex(audioData[i + fromIndex], 0);
        }

        CalculateFft(fftBuffer.AsSpan());

        for (int i = 0; i < FftHeight; i++)
            newFft[i] = MagnitudeSquared(fftBuffer[i]);

        // Pool the array to reduce the size with averaging the values
        int operatingIndex = 0;
        for (int i = 0; i < FftHeight; i += PoolingSize)
        {
            float averageValue = 0;
            for (int j = 0; j < PoolingSize; j++)
                if (i + j < FftHeight)
                    averageValue += newFft[i + j];

            newFft[operatingIndex] = averageValue / PoolingSize;
            operatingIndex++;
        }

        for (int i = 0; i < PooledFftHeight; i++)
            pooledFft[i] = (float)Math.Log10(newFft[i] + EPSILON);

        return pooledFft;
    }

    protected float MagnitudeSquared(Complex complex)
    {
        return (float)(complex.Real * complex.Real + complex.Imaginary * complex.Imaginary);
    }

    /// <summary>
    /// High performance FFT function.
    /// Complex input will be transformed in place.
    /// Input array length must be a power of two. This length is NOT validated.
    /// Running on an array with an invalid length may throw an invalid index exception.
    /// <para>Copy of the method from the FftSharp library https://github.com/swharden/FftSharp/blob/main/src/FftSharp/FftOperations.cs</para> 
    /// </summary>
    protected void CalculateFft(Span<Complex> buffer)
    {
        for (int i = 1; i < buffer.Length; i++)
        {
            int j = BitReverse(i, buffer.Length);
            if (j > i)
                (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
        }

        for (int i = 1; i <= buffer.Length / 2; i *= 2)
        {
            double mult1 = -Math.PI / i;
            for (int j = 0; j < buffer.Length; j += i * 2)
            {
                for (int k = 0; k < i; k++)
                {
                    int evenI = j + k;
                    int oddI = j + k + i;
                    Complex temp = new(Math.Cos(mult1 * k), Math.Sin(mult1 * k));
                    temp *= buffer[oddI];
                    buffer[oddI] = buffer[evenI] - temp;
                    buffer[evenI] += temp;
                }
            }
        }
    }

    /// <summary>
    /// Reverse the order of bits in an integer
    /// <para>Copy of the method from the FftSharp library https://github.com/swharden/FftSharp/blob/main/src/FftSharp/FftOperations.cs</para> 
    /// </summary>
    protected int BitReverse(int value, int maxValue)
    {
        int maxBitCount = (int)Math.Log(maxValue, 2);
        int output = value;
        int bitCount = maxBitCount - 1;

        value >>= 1;
        while (value > 0)
        {
            output = output << 1 | value & 1;
            bitCount -= 1;
            value >>= 1;
        }

        return output << bitCount & (1 << maxBitCount) - 1;
    }

    protected double[] CreateHannPeriodicWindow(int width)
    {
        double num = Math.PI * 2.0 / width;
        double[] array = new double[width];
        for (int i = 0; i < array.Length; i++)
            array[i] = 0.5 - 0.5 * Math.Cos(i * num);

        return array;
    }
}
