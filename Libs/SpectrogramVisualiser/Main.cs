using DigitalAssistant.Base.Audio;
using DigitalAssistant.Base.General;
using NAudio.Wave;
using Spectrogram;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SpectrogramVisualiser;

public partial class Main : Form
{
    #region Members
    private int FftSize = 512;
    private int WindowSize = 320;
    private int StepSize = 160;
    private int PoolingSize = 6;
    private int FftHeight = 257;
    private int PooledFftHeight = 43;
    private const float EPSILON = 1e-6f;

    private ulong AudioTotalSamples = 0;
    private float AudioAverage = 0;
    private float AudioMax = 0;

    private float OverallMinValue = float.MaxValue;
    private float OverallMaxValue = 0;

    private BufferList<float> AudioBuffer = new();
    private float[] HannWindow = [];
    private readonly object Lock = new();
    private readonly ImageMaker ImageMaker = new() { Colormap = Colormap.Viridis };

    private List<float[]> Ffts = [];

    private bool AudioFileViewMode = false;
    private bool AudioIsPlaying = false;
    private WaveInEvent WaveInEvent = new WaveInEvent
    {
        WaveFormat = new WaveFormat(rate: 16000, bits: 16, channels: 1),
    };
    #endregion

    public Main()
    {
        InitializeComponent();

        if (WaveIn.DeviceCount == 0)
        {
            MessageBox.Show("No microphone detected.\n\nThe program will now close.", "No microphone", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        WaveInEvent.DataAvailable += WaveIn_DataAvailable;
        WaveInEvent.StartRecording();
        AudioIsPlaying = true;
        ProgressBar.Visible = false;
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        var values = new Int16[e.Buffer.Length / 2];
        Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);

        float sum = 0;
        foreach (var sample in values)
            sum += sample;

        AudioTotalSamples += (ulong)values.Length;
        AudioAverage = (AudioTotalSamples * AudioAverage + sum) / (AudioTotalSamples + 1);

        var audioFloats = values.Select(x => (float)x).ToArray();
        for (int i = 0; i < audioFloats.Length; i++)
            audioFloats[i] -= AudioAverage;

        foreach (var sample in audioFloats)
            if (sample > AudioMax)
                AudioMax = sample;

        if (AudioMax > 0)
            for (int i = 0; i < audioFloats.Length; i++)
                audioFloats[i] /= AudioMax;

        lock (Lock)
        {
            AudioBuffer.AddRange(audioFloats);
        }
    }

    private void StopPlayAudioBtn_Click(object sender, EventArgs e)
    {
        if (AudioIsPlaying)
            WaveInEvent.StopRecording();
        else
        {
            if (AudioFileViewMode)
            {
                PictureBoxPanel.AutoScroll = false;
                PictureBox.SizeMode = PictureBoxSizeMode.Normal;
                PictureBox.Dock = DockStyle.Fill;
                AudioFileViewMode = false;
                Timer.Start();
            }

            WaveInEvent.StartRecording();
        }

        AudioIsPlaying = !AudioIsPlaying;
        StopPlayAudioBtn.Text = AudioIsPlaying ? "Stop" : "Play";
    }

    private void ViewAudioFileBtn_Click(object sender, EventArgs e)
    {
        if (OpenFileDialog.ShowDialog() != DialogResult.OK)
            return;

        Timer.Stop();
        if (AudioIsPlaying)
            StopPlayAudioBtn_Click(this, EventArgs.Empty);

        var audioSamples = ReadWavMono(OpenFileDialog.FileName, out int sampleRate);
        audioSamples = audioSamples[100..];
        lock (Lock)
        {
            PictureBoxPanel.AutoScroll = true;
            PictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            PictureBox.Dock = DockStyle.None;
            AudioFileViewMode = true;

            float sum = 0;
            foreach (var sample in audioSamples)
                sum += sample;

            AudioTotalSamples = (ulong)audioSamples.Length;
            AudioAverage = sum / (float)audioSamples.Length;

            for (int i = 0; i < audioSamples.Length; i++)
                audioSamples[i] -= AudioAverage;

            AudioMax = 0;
            foreach (var sample in audioSamples)
                if (sample > AudioMax)
                    AudioMax = sample;

            if (AudioMax > 0)
                for (int i = 0; i < audioSamples.Length; i++)
                    audioSamples[i] /= AudioMax;

            AudioBuffer.Clear();
            AudioBuffer.AddRangeSpan(audioSamples);
            Ffts.Clear();
            ProgressBar.Visible = true;
            CalculateFfts();

            var doubleFfts = Ffts.Select(floatArray => Array.ConvertAll(floatArray, f => (double)f)).ToList();
            using var fftBitmap = ImageMaker.GetBitmap(doubleFfts);

            // Match correct scaling so we keep aspect ratio
            var heightFactor = (float)PictureBoxPanel.Height / PooledFftHeight;
            var newWidth = (int)Math.Round(fftBitmap.Width * heightFactor);

            var bitmap = new Bitmap(newWidth, PictureBoxPanel.Height, PixelFormat.Format32bppPArgb);
            using var graphic = Graphics.FromImage(bitmap);
            graphic.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphic.DrawImage(fftBitmap, 0, 0, newWidth, PictureBoxPanel.Height);

            PictureBox.Image?.Dispose();
            PictureBox.Image = bitmap;
        }
    }

    private void WindowSize_ValueChanged(object sender, EventArgs e)
    {
        if (WindowStrideCtr.Value > WindowSizeCtr.Value)
        {
            WindowSizeCtr.Value = WindowSize;
            MessageBox.Show("Stride size cannot be larger than window size.", "Invalid window size", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        lock (Lock)
        {
            int fftSize = 1;
            while (fftSize < WindowSizeCtr.Value) // fft size needs to be power of 2, for good performance
                fftSize <<= 1;

            FftSize = fftSize;
            FftSizeCtr.Value = FftSize;
            WindowSize = (int)WindowSizeCtr.Value;

            RefreshCalculatedSettings();
        }
    }

    private void WindowStride_ValueChanged(object sender, EventArgs e)
    {
        if (WindowStrideCtr.Value > WindowSizeCtr.Value)
        {
            WindowStrideCtr.Value = StepSize;
            MessageBox.Show("Stride size cannot be larger than window size.", "Invalid stride size", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        lock (Lock)
        {
            StepSize = (int)WindowStrideCtr.Value;
            RefreshCalculatedSettings();
        }
    }

    private void PoolingSizeCtr_ValueChanged(object sender, EventArgs e)
    {
        lock (Lock)
        {
            PoolingSize = (int)PoolingSizeCtr.Value;
            RefreshCalculatedSettings();
        }
    }

    private void RefreshCalculatedSettings()
    {
        FftHeight = FftSize / 2 + 1;
        PooledFftHeight = (int)Math.Ceiling(FftHeight / (float)PoolingSize);
        Ffts.Clear();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        lock (Lock)
        {
            CalculateFfts();
            UpdatePictureBox();
        }
    }

    private void CalculateFfts()
    {
        var count = AudioBuffer.Count;
        var windowSize = WindowSize;
        var stepSize = StepSize;

        var fftsToProcess = (int)((count - windowSize) / (double)stepSize + 1);
        if (fftsToProcess < 1)
            return;

        var audioSpectrogram = new AudioSpectrogram(WindowSize, StepSize, PoolingSize);

        ProgressBar.Value = 0;
        for (int i = 0; i < fftsToProcess; i++)
        {
            var audioWindow = AudioBuffer[..WindowSize].ToArray();
            AudioBuffer.RemoveRange(0, StepSize);
            var fft = audioSpectrogram.GetSpectrogram(audioWindow, UseHannWindow.Checked)[0];

            foreach (var value in fft)
            {
                if (value < OverallMinValue)
                    OverallMinValue = value;
                if (value > OverallMaxValue)
                    OverallMaxValue = value;
            }

            var overallMinValue = Math.Abs(OverallMinValue);
            if (OverallMinValue > 0)
                overallMinValue = 0;

            var maxValue = OverallMaxValue + overallMinValue;

            for (int y = 0; y < fft.Length; y++)
                fft[y] = (fft[y] + overallMinValue) / maxValue * 255;

            Ffts.Add(fft);

            if (ProgressBar.Visible)
                ProgressBar.Value = (int)(i / (double)fftsToProcess * 100d);
        }

        ProgressBar.Visible = false;
    }

    private void UpdatePictureBox()
    {
        // Match correct scaling so we keep aspect ratio
        var heightFactor = (double)PictureBox.Height / PooledFftHeight;
        var noOfFftsForGivenHeightAndWithNeeded = (int)Math.Round(PictureBox.Width / heightFactor);

        while (Ffts.Count < noOfFftsForGivenHeightAndWithNeeded)
            Ffts.Insert(0, new float[PooledFftHeight]);
        while (Ffts.Count > noOfFftsForGivenHeightAndWithNeeded)
            Ffts.RemoveAt(0);

        var bitmap = new Bitmap(PictureBox.Width, PictureBox.Height, PixelFormat.Format32bppPArgb);
        var doubleFfts = Ffts.Select(floatArray => Array.ConvertAll(floatArray, f => (double)f)).ToList();
        using var fftBitmap = ImageMaker.GetBitmap(doubleFfts);
        using var graphic = Graphics.FromImage(bitmap);
        graphic.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphic.DrawImage(fftBitmap, 0, 0, PictureBox.Width, PictureBox.Height);

        PictureBox.Image?.Dispose();
        PictureBox.Image = bitmap;
    }

    #region MISC

    Span<float> ReadWavMono(string filePath, out int sampleRate)
    {
        using var afr = new AudioFileReader(filePath);
        sampleRate = afr.WaveFormat.SampleRate;
        int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
        int sampleCount = (int)(afr.Length / bytesPerSample);
        int channelCount = afr.WaveFormat.Channels;
        var audio = new BufferList<float>(sampleCount);
        var buffer = new float[sampleRate * channelCount];
        int samplesRead = 0;
        while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
            audio.AddRange(buffer.Take(samplesRead).Select(x => x * 32768f));

        return audio.AsSpan();
    }

#endregion
}
