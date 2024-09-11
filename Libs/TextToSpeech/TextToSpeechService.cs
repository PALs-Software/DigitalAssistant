using DigitalAssistant.Base.General;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace TextToSpeech;

public class TextToSpeechService : IDisposable
{
    #region Injects
    protected readonly ILogger<TextToSpeechService> Logger;
    #endregion

    #region Members
    protected int Language;
    protected string? ModelPath;
    protected InferenceSession? Session;
    protected ModelConfiguration? ModelConfiguration;
    protected SessionOptions? SessionOptions;
    protected SemaphoreSlim Semaphore = new(1, 1);
    protected TextToSpeechConfiguration TextToSpeechConfiguration;

    protected const char Padding = '_';
    protected const char SentenceStart = '^';
    protected const char SentenceEnd = '$';
    #endregion

    public TextToSpeechService(TextToSpeechConfiguration configuration, ILogger<TextToSpeechService> logger)
    {
        TextToSpeechConfiguration = configuration;
        Logger = logger;
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
        if (TextToSpeechConfiguration.PreventLoadingAiModels)
            return;

        ModelPath = GetModelPath(TextToSpeechConfiguration.Model);
        if (!File.Exists(ModelPath))
            return;

        try
        {
            var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ArgumentNullException.ThrowIfNull(executionDirectory);
            var initializeResult = Phonemize.InitWrapper(Path.Combine(executionDirectory, "espeak-ng-data"));
            if (initializeResult < 0)
                throw new Exception($"Can not initialize espeak. \"espeak_Initialize\" returns error code \"{initializeResult}\"");

            var jsonText = File.ReadAllText($"{ModelPath}.json", Encoding.UTF8);
            ModelConfiguration = JsonSerializer.Deserialize<ModelConfiguration>(jsonText);

            if (TextToSpeechConfiguration.UseGpu)
                SessionOptions = SessionOptions.MakeSessionOptionWithCudaProvider();
            else
                SessionOptions = new SessionOptions();

            SessionOptions.EnableMemoryPattern = false;
            SessionOptions.EnableCpuMemArena = false;
            SessionOptions.EnableProfiling = false;
            SessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;

            SessionOptions.RegisterOrtExtensions();
            Session = new InferenceSession(ModelPath, SessionOptions);
        }
        catch (Exception e)
        {
            Logger.LogError(e, PrepareExceptionErrorMessage(e));
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
            Logger.LogError(e, PrepareExceptionErrorMessage(e));
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
            var phonemes = Phonemize.ConvertTextToPhonemes(text, ModelConfiguration.Espeak.Voice);

            var result = new BufferList<float>();
            foreach (var sentencePhonemes in phonemes)
            {
                var phonemeIds = ConvertPhonemesToIds(sentencePhonemes, ModelConfiguration.PhonemeMapping);
                var inputs = new List<NamedOnnxValue> {
                    NamedOnnxValue.CreateFromTensor("input", new DenseTensor<Int64>(phonemeIds, [1, phonemeIds.Length])),
                    NamedOnnxValue.CreateFromTensor("input_lengths", new DenseTensor<Int64>(new long[]{ phonemeIds.Length}, [1])),
                    NamedOnnxValue.CreateFromTensor("scales", new DenseTensor<float>(new float[]{0.667f, 1f, 0.8f}, [3])),
                };

                using var outputs = Session.Run(inputs);
                if (outputs.Count == 0)
                    continue;

                result.AddRange(outputs[0].AsTensor<float>());
            }

            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, PrepareExceptionErrorMessage(e));
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

    public string? GetModelPath(string? ttsModel)
    {
        if (ttsModel == null)
            return null;

        return Path.Join(TextToSpeechConfiguration.ModelBaseDirectoryPath, ttsModel);
    }

    protected virtual long[] ConvertPhonemesToIds(string? phonemes, Dictionary<char, long[]> mapping)
    {
        if (phonemes == null)
            return [];

        var result = new List<long>
        {
            mapping[SentenceStart][0]
        };

        foreach (var phoneme in phonemes)
        {
            if (mapping.TryGetValue(phoneme, out long[]? ids) && ids.Length > 0)
            {
                result.Add(ids[0]);
                result.Add(mapping[Padding][0]);
            }
#if DEBUG
            else
                Debug.WriteLine($"Missing phoneme \"{phoneme}\" in mapping dictionary");
#endif
        }

        result.Add(mapping[SentenceEnd][0]);
        return result.ToArray();
    }

    protected virtual string PrepareExceptionErrorMessage(Exception e)
    {
        if (e.InnerException == null)
            return e.Message;

        return e.Message + Environment.NewLine + Environment.NewLine + "Inner Exception:" + PrepareExceptionErrorMessage(e.InnerException);
    }
    #endregion

}