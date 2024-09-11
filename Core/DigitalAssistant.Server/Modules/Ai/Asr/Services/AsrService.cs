using BlazorBase.Services;
using DigitalAssistant.Server.Modules.Ai.Asr.Enums;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Setups.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DigitalAssistant.Server.Modules.Ai.Asr.Services;

public class AsrService : IDisposable
{
    #region Injects
    protected readonly IConfiguration Configuration;
    protected readonly ILogger<AsrService> Logger;
    protected readonly BaseErrorHandler ErrorHandler;
    #endregion

    #region Members
    protected int Language;
    protected string? ModelPath;
    protected InferenceSession? Session;
    protected Microsoft.ML.OnnxRuntime.SessionOptions? SessionOptions;
    protected SemaphoreSlim Semaphore = new(1, 1);
    #endregion

    public AsrService(IConfiguration configuration, ILogger<AsrService> logger, BaseErrorHandler errorHandler)
    {
        Configuration = configuration;
        Logger = logger;
        ErrorHandler = errorHandler;

        InitSession();
    }


    public void Dispose()
    {
        Session?.Dispose();
        SessionOptions?.Dispose();
        Session = null;
        SessionOptions = null;
    }

    protected void InitSession()
    {
        var setup = Cache.SetupCache.Setup;
        if (setup == null)
            return;

        if (Configuration.GetValue<bool>("PreventLoadingAiModels"))
            return;

        Language = (int)setup.AsrLanguage;
        ModelPath = GetModelPath(setup);

        if (!File.Exists(ModelPath))
            return;

        try
        {
            SessionOptions = setup.AsrMode switch
            {
                AsrMode.Gpu => Microsoft.ML.OnnxRuntime.SessionOptions.MakeSessionOptionWithCudaProvider(),
                _ => new Microsoft.ML.OnnxRuntime.SessionOptions(),
            };
            SessionOptions.RegisterOrtExtensions();

            Session = new InferenceSession(ModelPath, SessionOptions);
        }
        catch (Exception e)
        {
            Logger.LogError(e, ErrorHandler.PrepareExceptionErrorMessage(e));
        }
    }

    public async Task ReInitModelAsync(CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Dispose();
            InitSession();
        }
        catch (Exception e)
        {
            Logger.LogError(e, ErrorHandler.PrepareExceptionErrorMessage(e));
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public Task<string?> ConvertSpeechToTextAsync(float[] samples, int sampleRate = 16000, CancellationToken cancellationToken = default)
    {
        return ConvertSpeechToTextAsync(samples.AsMemory(), sampleRate, cancellationToken);
    }

    public async Task<string?> ConvertSpeechToTextAsync(Memory<float> samples, int sampleRate = 16000, CancellationToken cancellationToken = default)
    {
        if (Session == null)
        {
            await ReInitModelAsync();
            if (Session == null)
                return null;
        }

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var batchSize = sampleRate * 30; // 30 seconds;
            var result = string.Empty;
            int startIndex = 0;

            while (startIndex < samples.Length)
            {
                var batch = samples.Slice(startIndex, Math.Min(batchSize, samples.Length - startIndex));
                startIndex += batchSize;

                var inputs = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor("audio_pcm", new DenseTensor<float>(batch, [1, batch.Length])),
                NamedOnnxValue.CreateFromTensor("max_length", new DenseTensor<int>(new int[]{448}, [1])),
                NamedOnnxValue.CreateFromTensor("min_length", new DenseTensor<int>(new int[]{1}, [1])),
                NamedOnnxValue.CreateFromTensor("num_beams", new DenseTensor<int>(new int[]{1}, [1])),
                NamedOnnxValue.CreateFromTensor("num_return_sequences", new DenseTensor<int>(new int[]{1}, [1])),
                NamedOnnxValue.CreateFromTensor("length_penalty", new DenseTensor<float>(new float[]{1f}, [1])),
                NamedOnnxValue.CreateFromTensor("repetition_penalty", new DenseTensor<float>(new float[]{1f}, [1])),
                NamedOnnxValue.CreateFromTensor("decoder_input_ids", new DenseTensor<int>(new int[] {
                        50258,      // start token id
                        Language,   // language id
                        50359,      // task transcribe (for task translate it would be 50358)
                        50363       // no timestamps (remove if model support timestamps)
                    }, [1, 4])),
                };

                using var outputs = Session.Run(inputs);
                if (outputs.Count == 0)
                    continue;

                result += outputs[0].AsTensor<string>().FirstOrDefault();
            }

            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, ErrorHandler.PrepareExceptionErrorMessage(e));
        }
        finally
        {
            Semaphore.Release();
        }

        return null;
    }

    #region MISC
    public string GetModelPath(Setup setup)
    {
        return Path.Join(Configuration["ModelsDirectoryPath"],
            "AsrModels",
            setup.AsrModel.ToString(),
            setup.AsrMode.ToString(),
            setup.AsrPrecision.ToString(),
            $"whisper_{setup.AsrModel.ToString().ToLower()}_{setup.AsrMode.ToString().ToLower()}_{setup.AsrPrecision.ToString().ToLower()}.onnx");
    }
    #endregion
}