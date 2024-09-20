using BlazorBase.Services;
using DigitalAssistant.Base.General;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Models;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Setups.Models;
using ESpeakWrapper;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Ai.TextToSpeech.Services;

public class TtsService : IDisposable
{
    #region Injects
    protected readonly ILogger<TtsService> Logger;
    protected readonly BaseErrorHandler ErrorHandler;
    #endregion

    #region Members
    protected int Language;
    protected string? ModelPath;
    protected InferenceSession? Session;
    protected TtsConfiguration? ModelConfiguration;
    protected Microsoft.ML.OnnxRuntime.SessionOptions? SessionOptions;
    protected SemaphoreSlim Semaphore = new(1, 1);

    protected const char Padding = '_';
    protected const char SentenceStart = '^';
    protected const char SentenceEnd = '$';

    protected bool PreventLoadingAiModels;
    protected string ModelsDirectoryPath;
    #endregion

    public TtsService(IConfiguration configuration, ILogger<TtsService> logger, BaseErrorHandler errorHandler)
    {
        Logger = logger;
        ErrorHandler = errorHandler;

        PreventLoadingAiModels = configuration.GetValue<bool>("PreventLoadingAiModels");
        ModelsDirectoryPath = configuration["ModelsDirectoryPath"] ?? String.Empty;

        InitSession();
    }

    public void Dispose()
    {
        Session?.Dispose();
        SessionOptions?.Dispose();
        Session = null;
        SessionOptions = null;
    }

    protected virtual void InitSession()
    {
        var setup = Cache.SetupCache.Setup;
        if (setup == null)
            return;

        if (PreventLoadingAiModels)
            return;

        ModelPath = GetModelPath(setup);
        if (!File.Exists(ModelPath))
            return;

        try
        {
            var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ArgumentNullException.ThrowIfNull(executionDirectory);
            var espeakNgDataPath = Path.Combine(executionDirectory, "espeak-ng-data");

            var jsonText = File.ReadAllText($"{ModelPath}.json", Encoding.UTF8);
            ModelConfiguration = JsonSerializer.Deserialize<TtsConfiguration>(jsonText);

            var initializeResult = ESpeakWrapperInstance.Initialize(espeakNgDataPath);
            if (initializeResult < 0)
                throw new Exception($"Can not initialize espeak, error code: \"{initializeResult}\"");

            ArgumentNullException.ThrowIfNull(ModelConfiguration?.Espeak?.Voice);
            var setVoiceResult = ESpeakWrapperInstance.SetVoiceByName(ModelConfiguration.Espeak.Voice);
            if (setVoiceResult != 0)
                throw new Exception($"Can not set espeak voice, error code: \"{setVoiceResult}\"");

            SessionOptions = setup.TtsMode switch
            {
#if GPUSUPPORTENABLED
                Enums.TtsMode.Gpu => Microsoft.ML.OnnxRuntime.SessionOptions.MakeSessionOptionWithCudaProvider(),
#endif
                _ => new Microsoft.ML.OnnxRuntime.SessionOptions(),
            };

            SessionOptions.EnableMemoryPattern = false;
            SessionOptions.EnableCpuMemArena = false;
            SessionOptions.EnableProfiling = false;
            SessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;

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
            Session = null;
            ModelConfiguration = null;
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

    public async Task<BufferList<float>?> ConvertTextToSpeechAsync(string text, CancellationToken cancellationToken = default)
    {
        if (Session == null || ModelConfiguration == null)
        {
            await ReInitModelAsync();
            if (Session == null || ModelConfiguration == null)
                return null;
        }

        ArgumentNullException.ThrowIfNull(ModelConfiguration.Espeak.Voice);

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = new BufferList<float>();
            ESpeakWrapperInstance.ConvertTextToPhonemes(text, out var success, out var phonemes, out var error);
            if (!success)
                throw new Exception(error);

            var phonemeIds = ConvertPhonemesToIds(phonemes, ModelConfiguration.PhonemeMapping);
            var inputs = new List<NamedOnnxValue> {
                    NamedOnnxValue.CreateFromTensor("input", new DenseTensor<long>(phonemeIds, [1, phonemeIds.Length])),
                    NamedOnnxValue.CreateFromTensor("input_lengths", new DenseTensor<long>(new long[]{ phonemeIds.Length}, [1])),
                    NamedOnnxValue.CreateFromTensor("scales", new DenseTensor<float>(new float[]{0.667f, 1f, 0.8f}, [3])),
                };

            using var outputs = Session.Run(inputs);
            if (outputs.Count != 0)
                result.AddRange(outputs[0].AsTensor<float>());

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

    public int? GetCurrentModelSampleRate()
    {
        return ModelConfiguration?.Audio.SampleRate;
    }

    #region MISC

    public string GetModelPath(Setup setup)
    {
        return Path.Join(ModelsDirectoryPath,
            "TtsModels",
            $"{setup.TtsLanguage}-{setup.TtsModel}-{setup.TtsModelQuality}.onnx");
    }

    protected long[] ConvertPhonemesToIds(string? phonemes, Dictionary<char, long[]> mapping)
    {
        if (phonemes == null)
            return [];

        var result = new List<long>();
        var sentences = phonemes.Split([". ", "? ", "? "], StringSplitOptions.RemoveEmptyEntries);

        foreach (var sentence in sentences)
        {
            result.Add(mapping[SentenceStart][0]);

            foreach (var phoneme in sentence)
            {
                if (mapping.TryGetValue(phoneme, out long[]? ids) && ids.Length > 0)
                {
                    result.Add(ids[0]);
                    result.Add(mapping[Padding][0]);
                }
#if DEBUG
                else
                    System.Diagnostics.Debug.WriteLine($"Missing phoneme \"{phoneme}\" in mapping dictionary");
#endif
            }

            result.Add(mapping[SentenceEnd][0]);
        }

        return result.ToArray();
    }
    #endregion

}