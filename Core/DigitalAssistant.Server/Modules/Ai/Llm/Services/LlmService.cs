using BlazorBase.Services;
using DigitalAssistant.Server.Modules.Ai.Llm.Enums;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Setups.Models;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace DigitalAssistant.Server.Modules.Ai.Llm.Services;

public class LlmService : IDisposable
{
    #region Injects
    protected readonly LlmModelSelectionService ModelSelectionService;
    protected readonly ILogger<LlmService> Logger;
    protected readonly BaseErrorHandler ErrorHandler;
    #endregion

    #region Members
    protected string? ModelPath;
    protected Model? Model;
    protected Tokenizer? Tokenizer;
    protected SemaphoreSlim Semaphore = new(1, 1);

    protected bool PreventLoadingAiModels;
    protected string ModelsDirectoryPath;
    #endregion

    public LlmService(IConfiguration configuration, LlmModelSelectionService modelSelectionService, ILogger<LlmService> logger, BaseErrorHandler errorHandler)
    {
        ModelSelectionService = modelSelectionService;
        Logger = logger;
        ErrorHandler = errorHandler;

        PreventLoadingAiModels = configuration.GetValue<bool>("PreventLoadingAiModels");
        ModelsDirectoryPath = configuration["ModelsDirectoryPath"] ?? String.Empty;

        InitSession();
    }


    public void Dispose()
    {
        Model?.Dispose();
        Tokenizer?.Dispose();
        Model = null;
        Tokenizer = null;
    }

    protected void InitSession()
    {
        var setup = Cache.SetupCache.Setup;
        if (setup == null)
            return;

        if (PreventLoadingAiModels)
            return;

        ModelPath = GetModelDirectoryPath(setup);
        if (!Directory.Exists(ModelPath))
            return;

        try
        {
            Model = new Model(ModelPath);
            Tokenizer = new Tokenizer(Model);
        }
        catch (Exception e)
        {
            Dispose();
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

    public async Task<string?> GenerateAnswerAsync(string systemPrompt, string userPrompt, string? stopToken = null, int maxLength = 512, CancellationToken cancellationToken = default)
    {
        if (Model == null || Tokenizer == null)
        {
            await ReInitModelAsync();
            if (Model == null || Tokenizer == null)
                return null;
        }

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var fullPrompt = $"<|system|>{systemPrompt}<|end|><|user|>{userPrompt}<|end|><|assistant|>";
            using var tokenizerStream = Tokenizer.CreateStream();
            using var tokens = Tokenizer.Encode(fullPrompt);

            using var generatorParams = new GeneratorParams(Model);
            generatorParams.SetSearchOption("length_penalty", 0.5); // Prefer shorter answers (everything smaller 1)
            generatorParams.SetSearchOption("early_stopping", true); // Whether to stop the beam search when at least num_beams sentences are finished per batch or not
            generatorParams.SetSearchOption("do_sample", false); // Enables Top P / Top K generation
            generatorParams.SetSearchOption("temperature", 0.6); // The temperature value scales the scores of each token so that lower a temperature value leads to a sharper distribution.
            generatorParams.SetInputSequences(tokens);

            using var generator = new Generator(Model, generatorParams);
            var result = String.Empty;

#if DEBUG
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
#endif

            while (!generator.IsDone())
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();
                var token = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                result += token;
                if (result.Length > maxLength || (stopToken != null && token.Contains(stopToken)))
                    break;
            }

#if DEBUG
            stopwatch.Stop();
            Logger.LogInformation("Llm generation request with {InputCharactersLength} input characters took {ElapsedMilliseconds}ms for generating {OutputCharactersLength} characters", fullPrompt.Length, stopwatch.ElapsedMilliseconds, result.Length);
#endif
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

    public Dictionary<LlmFileType, string> GetModelPaths(Setup setup)
    {
        var result = new Dictionary<LlmFileType, string>();
        var fileTypes = Enum.GetValues<LlmFileType>();
        foreach (var fileType in fileTypes)
            result.Add(fileType, GetModelPath(fileType, setup));

        return result;
    }

    public string GetModelPath(LlmFileType fileType, Setup setup)
    {
        return Path.Join(ModelsDirectoryPath,
            "LlmModels",
            setup.LlmModel.ToString(),
            GetExecutionMode(setup),
            setup.LlmPrecision.ToString(),
            ModelSelectionService.GetFileName(fileType, setup.LlmModel, setup.LlmContextSize, GetExecutionMode(setup.LlmMode), setup.LlmPrecision));
    }

    public string GetModelDirectoryPath(Setup setup)
    {
        return Path.Join(ModelsDirectoryPath,
            "LlmModels",
            setup.LlmModel.ToString(),
            GetExecutionMode(setup),
            setup.LlmPrecision.ToString());
    }

    private string GetExecutionMode(Setup setup)
    {
#if GPUSUPPORTENABLED
        return setup.LlmMode.ToString();
#else
        return "Cpu";
#endif
    }

    private LlmMode GetExecutionMode(LlmMode mode)
    {
#if GPUSUPPORTENABLED
        return mode;
#else
        return LlmMode.Cpu;
#endif
    }
    #endregion
}