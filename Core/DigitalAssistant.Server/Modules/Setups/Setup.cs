using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Models;
using BlazorBase.MessageHandling.Enum;
using BlazorBase.MessageHandling.Interfaces;
using DigitalAssistant.Abstractions.Commands.Enums;
using DigitalAssistant.Server.Modules.Ai.Asr.Enums;
using DigitalAssistant.Server.Modules.Ai.Asr.Services;
using DigitalAssistant.Server.Modules.Ai.Llm.Enums;
using DigitalAssistant.Server.Modules.Ai.Llm.Services;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Enums;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Models;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Services;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.MessageHandling.Components;
using DigitalAssistant.Server.Modules.Setups.Enums;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DigitalAssistant.Server.Modules.Setups.Models;

[Route("/Setup")]
[Authorize(Roles = "Admin")]
public partial class Setup : BaseModel
{
    #region Init

    public Setup() { }
    public Setup(IServiceProvider serviceProvider, Action<Setup> configureOptions)
    {
        Cache.SetupCache.Setup?.TransferPropertiesTo(this);
    }

    #endregion

    #region Properties

    #region Primary Key

    [Key]
    [Required]
    public Guid Id { get; set; }
    #endregion

    #region General
    public bool InitalSetupCompleted { get; set; }
    #endregion

    #region Asr Model

    [Visible(DisplayGroup = "Speech Recognition Model", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public AsrProvider AsrProvider { get; set; } = AsrProvider.Whisper;

    [Visible(DisplayGroup = "Speech Recognition Model", DisplayOrder = 200)]
    public AsrLanguages AsrLanguage { get; set; } = AsrLanguages.English;

    [Visible(DisplayGroup = "Speech Recognition Model", DisplayOrder = 300)]
    public AsrModels AsrModel { get; set; } = AsrModels.Tiny;

    [Visible(DisplayGroup = "Speech Recognition Model", DisplayOrder = 400)]
    public AsrMode AsrMode { get; set; } = AsrMode.Cpu;

    [Visible(DisplayGroup = "Speech Recognition Model", DisplayOrder = 500)]
    public AsrPrecision AsrPrecision { get; set; } = AsrPrecision.FP32;

    #endregion

    #region TTS Model

    [Visible(DisplayGroup = "Text to Speech Model", DisplayGroupOrder = 200, DisplayOrder = 100)]
    public TtsLanguages TtsLanguage { get; set; } = TtsLanguages.English_US;

    [Required]
    [UseCustomLookupData(nameof(GetTtsModelsForSelectedLanguage))]
    [Visible(DisplayGroup = "Text to Speech Model", DisplayOrder = 200)]
    public string TtsModel { get; set; } = String.Empty;

    [Required]
    [UseCustomLookupData(nameof(GetTtsModelQualitiesForSelectedModel))]
    [Visible(DisplayGroup = "Text to Speech Model", DisplayGroupOrder = 100, DisplayOrder = 300)]
    public TtsModelQuality TtsModelQuality { get; set; }

    [Visible(DisplayGroup = "Text to Speech Model", DisplayOrder = 400)]
    public TtsMode TtsMode { get; set; } = TtsMode.Cpu;

    #endregion

    #region Llm Model

    [Visible(DisplayGroup = "Large Language Model", DisplayGroupOrder = 300, DisplayOrder = 100)]
    public LlmModels LlmModel { get; set; } = LlmModels.Disabled;

    [Visible(DisplayGroup = "Large Language Model", DisplayOrder = 200)]
    public LlmMode LlmMode { get; set; } = LlmMode.Cpu;

    [Visible(DisplayGroup = "Large Language Model", DisplayOrder = 300)]
    public LlmPrecision LlmPrecision { get; set; } = LlmPrecision.INT4Accelerated;

    [Visible(DisplayGroup = "Large Language Model", DisplayOrder = 400)]
    public LlmContextSize LlmContextSize { get; set; } = LlmContextSize.Context4k;

    #endregion

    #region Interpreter

    [Visible(DisplayGroup = "Interpreter", DisplayGroupOrder = 400, DisplayOrder = 100)]
    public InterpreterMode InterpreterMode { get; set; } = InterpreterMode.RegularExpression;

    [Visible(DisplayGroup = "Interpreter", DisplayOrder = 200)]
    public InterpreterLanguages InterpreterLanguage { get; set; } = InterpreterLanguages.English;

    #endregion

    #endregion

    #region Members
    private bool AsrSettingsChanged = false;
    private bool TtsSettingsChanged = false;
    private bool LlmSettingsChanged = false;
    private bool SkipLoadingOfChangedModels = false;
    #endregion

    #region CRUD

    public override async Task OnBeforeAddEntry(OnBeforeAddEntryArgs args)
    {
        Id = await args.EventServices.DbContext.GetNewPrimaryKeyAsync(GetType());

        var success = await DownloadModelsAsync(args.EventServices);
        args.AbortAdding = !success;
    }

    public override async Task OnBeforeUpdateEntry(OnBeforeUpdateEntryArgs args)
    {
        await base.OnBeforeUpdateEntry(args);

        var success = await DownloadModelsAsync(args.EventServices);
        args.AbortUpdating = !success;
    }

    protected async Task<bool> DownloadModelsAsync(EventServices eventServices)
    {
        bool success = true;
        var serviceProvider = eventServices.ServiceProvider;
        var localizer = eventServices.Localizer;
        var fileDownloadService = serviceProvider.GetRequiredService<FileDownloadService>();
        var tasks = new List<Task<bool>>();

        if (AsrSettingsChanged)
        {
            var asrService = serviceProvider.GetRequiredService<AsrService>();
            var modelSelectionService = serviceProvider.GetRequiredService<AsrModelSelectionService>();
            var asrModelPath = asrService.GetModelPath(this);
            if (!File.Exists(asrModelPath))
            {
                var modelUrl = modelSelectionService.GetCompleteDownloadLinkForModel(AsrModel, AsrMode, AsrPrecision);
                if (!await fileDownloadService.TestFileExistsAsync(modelUrl))
                    throw new CRUDException(localizer["AsrModelNotExistErr"]);

                tasks.Add(fileDownloadService.DownloadFileAsync(Path.GetFileName(asrModelPath), modelUrl, asrModelPath));
            }

            var asrExtendedModelDataPath = Path.Join(Path.GetDirectoryName(asrModelPath), "model_with_beam_search.onnx.data");
            var extendedModelDataUrl = modelSelectionService.GetCompleteDownloadLinkForModel(AsrModel, AsrMode, AsrPrecision, dataModel: true);
            if (!File.Exists(asrExtendedModelDataPath) && await fileDownloadService.TestFileExistsAsync(extendedModelDataUrl))
                tasks.Add(fileDownloadService.DownloadFileAsync(Path.GetFileName(asrExtendedModelDataPath), extendedModelDataUrl, asrExtendedModelDataPath));
        }

        if (TtsSettingsChanged)
        {
            var ttsService = serviceProvider.GetRequiredService<TtsService>();
            var ttsModelPath = ttsService.GetModelPath(this);
            var jsonModelPath = ttsModelPath + ".json";
            if (ttsModelPath != null)
            {
                var modelSelectionService = serviceProvider.GetRequiredService<TtsModelSelectionService>();
                var ttsModel = modelSelectionService.GetModel(TtsLanguage, TtsModel, TtsModelQuality);
                if (ttsModel != null)
                {
                    if (!File.Exists(ttsModelPath))
                    {
                        var ttsModelUrl = modelSelectionService.GetCompleteDownloadLinkForModel(ttsModel);
                        if (!await fileDownloadService.TestFileExistsAsync(ttsModelUrl))
                            throw new CRUDException(localizer["TtsModelNotExistErr"]);

                        tasks.Add(fileDownloadService.DownloadFileAsync(Path.GetFileName(ttsModelPath), ttsModelUrl, ttsModelPath));
                    }

                    if (!File.Exists(jsonModelPath))
                        tasks.Add(fileDownloadService.DownloadFileAsync(Path.GetFileName(jsonModelPath), modelSelectionService.GetCompleteDownloadLinkForModel(ttsModel, jsonFile: true), jsonModelPath));
                }
            }
        }

        if (LlmSettingsChanged && LlmModel != LlmModels.Disabled)
        {
            var modelSelectionService = serviceProvider.GetRequiredService<LlmModelSelectionService>();
            var llmService = serviceProvider.GetRequiredService<LlmService>();
            var llmModelPaths = llmService.GetModelPaths(this);
            var downloadLinks = modelSelectionService.GetCompleteDownloadLinkForAllFileTypes(LlmModel, LlmContextSize, LlmMode, LlmPrecision);
            if (downloadLinks == null)
                throw new CRUDException(localizer["LlmModelNotExistErr"]);

            foreach (var llmModelPath in llmModelPaths)
            {
                if (llmModelPath.Value != null && !File.Exists(llmModelPath.Value))
                {
                    var downloadLink = modelSelectionService.GetCompleteDownloadLinkForModel(llmModelPath.Key, LlmModel, LlmContextSize, LlmMode, LlmPrecision);
                    if (downloadLink == null || !await fileDownloadService.TestFileExistsAsync(downloadLink))
                        throw new CRUDException(localizer["LlmModelNotExistErr"]);

                    tasks.Add(fileDownloadService.DownloadFileAsync(Path.GetFileName(llmModelPath.Value), downloadLink, llmModelPath.Value));
                }
            }
        }

        if (tasks.Count > 0)
        {
            var results = await Task.WhenAll(tasks);
            success = !results.Any(result => !result);
            if (!success)
            {
                var messageHandler = serviceProvider.GetRequiredService<IMessageHandler>();
                messageHandler.ShowMessage(localizer["DownloadCancelledTitle"], localizer["DownloadCancelledMsg"], MessageType.Error);
            }
        }

        if (TtsSettingsChanged)
        {
            var ttsService = serviceProvider.GetRequiredService<TtsService>();
            var ttsModelPath = ttsService.GetModelPath(this);
            var jsonModelPath = ttsModelPath + ".json";
            var jsonText = File.ReadAllText(jsonModelPath, Encoding.UTF8);
            var modelConfiguration = JsonSerializer.Deserialize<TtsConfiguration>(jsonText);
            if (modelConfiguration == null || modelConfiguration.Audio.SampleRate == 0)
                throw new CRUDException(localizer["SampleRateCanNotBeReadErr", jsonModelPath]);

            var clientsWhichNeedsUpdate = await eventServices.DbContext
                .WhereAsync<Client>(entry => entry.VoiceAudioOutputSampleRate != modelConfiguration.Audio.SampleRate);

            foreach (var client in clientsWhichNeedsUpdate)
            {
                client.VoiceAudioOutputSampleRate = modelConfiguration.Audio.SampleRate;
                client.ClientNeedSettingsUpdate = true;
            }
        }

        return success;
    }

    public override async Task OnAfterCardSaveChanges(OnAfterCardSaveChangesArgs args)
    {
        await base.OnAfterCardSaveChanges(args);

        var serviceProvider = args.EventServices.ServiceProvider;
        await Cache.SetupCache.RefreshSetupCacheAsync(serviceProvider);

        if (SkipLoadingOfChangedModels)
            return;

        if (AsrSettingsChanged)
        {
            var asrService = serviceProvider.GetRequiredService<AsrService>();
            await asrService.ReInitModelAsync();
            AsrSettingsChanged = false;
        }

        if (LlmSettingsChanged)
        {
            var llmService = serviceProvider.GetRequiredService<LlmService>();
            await llmService.ReInitModelAsync();
            LlmSettingsChanged = false;
        }

        if (TtsSettingsChanged)
        {
            var ttsService = serviceProvider.GetRequiredService<TtsService>();
            await ttsService.ReInitModelAsync();
            TtsSettingsChanged = false;

            var clientsWhichNeedsUpdate = await args.EventServices.DbContext
                .WhereAsync<Client>(entry => entry.ClientNeedSettingsUpdate);
            if (clientsWhichNeedsUpdate.Count > 0)
            {
                foreach (var client in clientsWhichNeedsUpdate)
                {
                    var success = await client.UpdateSettingsOnClientAsync(args.EventServices, skipSettingClientNeedSettingsUpdateOnFailure: true);
                    client.ClientNeedSettingsUpdate = !success;
                }

                await args.EventServices.DbContext.SaveChangesAsync();
            }
        }
    }

    public override Task OnAfterValidateProperty(OnAfterValidatePropertyArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(LlmModel):
            case nameof(LlmMode):
            case nameof(LlmPrecision):
                if (LlmModel == LlmModels.Disabled)
                    break;

                if (LlmMode != LlmMode.Cpu && LlmPrecision == LlmPrecision.INT4Accelerated)
                    ShowUserInvalidConfiguration(args, args.EventServices.Localizer["LlmGpuPrecisionNotSupportedErr"]);

                if (LlmMode == LlmMode.Cpu && LlmPrecision == LlmPrecision.FP16)
                    ShowUserInvalidConfiguration(args, args.EventServices.Localizer["LlmCpuPrecisionNotSupportedErr"]);

                if (LlmModel == LlmModels.Medium && LlmMode == LlmMode.Cpu && LlmPrecision != LlmPrecision.INT4Accelerated)
                    ShowUserInvalidConfiguration(args, args.EventServices.Localizer["LlmMediumPrecisionNotSupportedErr"]);
                break;

            case nameof(InterpreterMode):
                if (InterpreterMode != InterpreterMode.RegularExpression && LlmModel == LlmModels.Disabled)
                    ShowUserInvalidConfiguration(args, args.EventServices.Localizer["InterpreterModeWrongErr"]);
                break;
            default:
                break;
        }

        return base.OnAfterValidateProperty(args);
    }

    private void ShowUserInvalidConfiguration(OnAfterValidatePropertyArgs args, string errorMessage)
    {
        args.IsValid = false;
        args.ErrorMessage = errorMessage;
    }

    public override Task OnAfterPropertyChanged(OnAfterPropertyChangedArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(AsrLanguage):
            case nameof(AsrModel):
            case nameof(AsrMode):
            case nameof(AsrPrecision):
                AsrSettingsChanged = true;
                break;

            case nameof(TtsLanguage):
                TtsSettingsChanged = true;
                RecalculateCustomLookupData(nameof(TtsModel));
                RecalculateCustomLookupData(nameof(TtsModelQuality));
                break;

            case nameof(TtsModel):
                TtsSettingsChanged = true;
                RecalculateCustomLookupData(nameof(TtsModelQuality));
                break;

            case nameof(TtsModelQuality):
            case nameof(TtsMode):
                TtsSettingsChanged = true;
                break;
            
            case nameof(LlmModel):
            case nameof(LlmMode):
            case nameof(LlmPrecision):
            case nameof(LlmContextSize):
                ForcePropertyRepaint(nameof(LlmModel), nameof(LlmMode), nameof(LlmPrecision), nameof(LlmContextSize));
                LlmSettingsChanged = true;
                break;
        }

        return base.OnAfterPropertyChanged(args);
    }

    public static Task GetTtsModelsForSelectedLanguage(PropertyInfo propertyInfo, IBaseModel cardModel, List<KeyValuePair<string?, string>> lookupData, EventServices eventServices)
    {
        var setup = ((Setup)cardModel);
        var language = setup.TtsLanguage;
        var modelSelectionService = eventServices.ServiceProvider.GetRequiredService<TtsModelSelectionService>();
        var models = modelSelectionService.GetModelsForLanguage(language).DistinctBy(entry => entry.Name).ToList();
        foreach (var model in models)
            lookupData.Add(new KeyValuePair<string?, string>(model.Name, model.Name));

        if (!models.Any(entry => entry.Name == setup.TtsModel))
        {
            var firstModel = models.First();
            setup.TtsModel = firstModel.Name;
            setup.TtsModelQuality = firstModel.Quality;
            setup.ForcePropertyRepaint(nameof(TtsModel), nameof(TtsModelQuality));
        }

        return Task.CompletedTask;
    }

    public static Task GetTtsModelQualitiesForSelectedModel(PropertyInfo propertyInfo, IBaseModel cardModel, List<KeyValuePair<string?, string>> lookupData, EventServices eventServices)
    {
        var setup = ((Setup)cardModel);
        var localizer = eventServices.ServiceProvider.GetRequiredService<IStringLocalizer<TtsModelQuality>>();
        var language = ((Setup)cardModel).TtsLanguage;
        var modelSelectionService = eventServices.ServiceProvider.GetRequiredService<TtsModelSelectionService>();
        var models = modelSelectionService.GetModelsForLanguage(language).Where(entry => entry.Name == setup.TtsModel).ToList();
        foreach (var model in models)
            lookupData.Add(new KeyValuePair<string?, string>(model.Quality.ToString(), localizer[model.Quality.ToString()]));

        if (!models.Any(entry => entry.Quality == setup.TtsModelQuality))
        {
            setup.TtsModelQuality = models.First().Quality;
            setup.ForcePropertyRepaint(nameof(TtsModelQuality));
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Inital Setup
    public void SetSettingsFromInitialSetup()
    {
        AsrSettingsChanged = true;
        TtsSettingsChanged = true;
        LlmSettingsChanged = true;
        SkipLoadingOfChangedModels = true;
    }

    public static async Task<bool> InitialSetupCompletedAsync(IBaseDbContext dbContext)
    {
        var setup = await dbContext.FirstOrDefaultAsync<Setup>(asNoTracking: true);
        var user = dbContext.FirstOrDefaultAsync<User>(asNoTracking: true);

        return setup != null && user != null && setup.InitalSetupCompleted;
    }

    public static async Task SetInitialSetupToCompletedAsync(IBaseDbContext dbContext)
    {
        var setup = await dbContext.FirstOrDefaultAsync<Setup>();
        if (setup == null)
        {
            setup = new Setup();
            await dbContext.AddAsync(setup);
        }

        setup.InitalSetupCompleted = true;
        await dbContext.SaveChangesAsync();
    }
    #endregion

    #region MISC
    public override string GetDisplayKey()
    {
        return String.Empty;
    }

    public override string GetDisplayKey(string seperator)
    {
        return String.Empty;
    }
    #endregion

}
