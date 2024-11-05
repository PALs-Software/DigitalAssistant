using BlazorBase.AudioRecorder.Services;
using DigitalAssistant.Base.General;
using NAudio.Wave;


var filePath = @"file.wav";

Console.WriteLine("Convert 16000 Hz, 16 Bit, Mono wav file, to binary without header for micro client!");
Console.WriteLine("Use \"xxd -i output.out > output.cpp\" under linux to convert to cpp file");

using var afr = new AudioFileReader(filePath);
var sampleRate = afr.WaveFormat.SampleRate;
int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
int sampleCount = (int)(afr.Length / bytesPerSample);
int channelCount = afr.WaveFormat.Channels;

var samples = new BufferList<float>(sampleCount);
var buffer = new float[sampleRate * channelCount];
int samplesRead = 0;
while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
    samples.AddRange(buffer.Take(samplesRead).Select(x => x * 32768f));

var audioConverter = new AudioConverter();
var shorts = audioConverter.ConvertFloatToShortSamples(samples.AsSpan(), withScaling: false);

var bytes = new byte[samples.Count * 2];
Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);

File.WriteAllBytes(Path.Join(Path.GetDirectoryName(filePath), $"{Path.GetFileNameWithoutExtension(filePath)}.out"), bytes);