using DigitalAssistant.Server.Modules.Ai.Llm.Enums;

namespace DigitalAssistant.Server.Modules.Ai.Llm.Services;

public class LlmModelSelectionService
{
    protected const string DownloadParameter = "?download=true";
    protected const string BaseUrl = "https://huggingface.co/microsoft/{0}/resolve/main";

    public List<string>? GetCompleteDownloadLinkForAllFileTypes(LlmModels model, LlmContextSize contextSize, LlmMode mode, LlmPrecision precision)
    {
        var result = new List<string>();
        var fileTypes = Enum.GetValues<LlmFileType>();
        foreach (var fileType in fileTypes)
        {
            var link = GetCompleteDownloadLinkForModel(fileType, model, contextSize, mode, precision);
            if (link == null)
                return null;
            result.Add(link);
        }

        return result;
    }

    public string? GetCompleteDownloadLinkForModel(LlmFileType file, LlmModels model, LlmContextSize contextSize, LlmMode mode, LlmPrecision precision)
    {
        var baseUrl = String.Format(BaseUrl, $"Phi-3-{GetModel(model)}-{GetContextSize(contextSize)}-instruct-onnx{GetModeUrlPart(model, mode)}");
        var innerFolderPath = GetInnerModeFolderPath(model, mode);
        var innerModelFolderPath = GetInnerModelFolderPath(mode, precision);
        var fileName = GetFileName(file, model, contextSize, mode, precision);

        if (baseUrl == null || innerModelFolderPath == null || fileName == null)
            return null;

        if (innerFolderPath != null)
            innerFolderPath = "/" + innerFolderPath;

        return $"{baseUrl}{innerFolderPath}/{innerModelFolderPath}/{fileName}{DownloadParameter}";
    }

    protected string? GetModeUrlPart(LlmModels model, LlmMode mode)
    {
        if (model == LlmModels.Mini)
            return String.Empty;
        else
            return $"-{GetMode(mode)}";
    }

    protected string? GetInnerModeFolderPath(LlmModels model, LlmMode mode)
    {
        if (model != LlmModels.Mini)
            return null;

        return mode switch
        {
            LlmMode.Cpu => "cpu_and_mobile",
#if GPUSUPPORTENABLED
          LlmMode.Gpu => "cuda",
#endif
            _ => null
        };
    }

    protected string? GetInnerModelFolderPath(LlmMode mode, LlmPrecision precision)
    {
        return $"{GetMode(mode)}-{GetPrecision(precision)}";
    }

    public string? GetFileName(LlmFileType file, LlmModels model, LlmContextSize contextSize, LlmMode mode, LlmPrecision precision)
    {
        return file switch
        {
            LlmFileType.Model => $"phi3-{GetModel(model)}-{GetContextSize(contextSize)}-instruct-{GetMode(mode)}-{GetPrecision(precision)}.onnx",
            LlmFileType.ModelData => $"phi3-{GetModel(model)}-{GetContextSize(contextSize)}-instruct-{GetMode(mode)}-{GetPrecision(precision)}.onnx.data",
            LlmFileType.Config => "genai_config.json",
            LlmFileType.Tokenizer => "tokenizer.json",
            LlmFileType.TokenizerConfig => "tokenizer_config.json",
            _ => null
        };
    }

    #region Misc
    protected string? GetModel(LlmModels model)
    {
        return model switch
        {
            LlmModels.Mini => "mini",
            LlmModels.Medium => "medium",
            _ => null
        };
    }

    protected string? GetContextSize(LlmContextSize contextSize)
    {
        return contextSize switch
        {
            LlmContextSize.Context4k => "4k",
            LlmContextSize.Context128k => "128k",
            _ => null
        };
    }

    protected string? GetMode(LlmMode mode)
    {
        return mode switch
        {
            LlmMode.Cpu => "cpu",
#if GPUSUPPORTENABLED
            LlmMode.Gpu => "cuda",
#endif
            _ => null
        };
    }

    protected string? GetPrecision(LlmPrecision precision)
    {
        return precision switch
        {
            LlmPrecision.INT4Accelerated => "int4-rtn-block-32-acc-level-4",
            LlmPrecision.INT4 => "int4-rtn-block-32",
            LlmPrecision.FP16 => "fp16",
            _ => null
        };
    }
    #endregion
}
