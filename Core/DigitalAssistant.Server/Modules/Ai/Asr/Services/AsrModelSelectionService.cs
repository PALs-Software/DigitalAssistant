using DigitalAssistant.Server.Modules.Ai.Asr.Enums;

namespace DigitalAssistant.Server.Modules.Ai.Asr.Services;

public class AsrModelSelectionService
{
    protected const string DownloadParameter = "?download=true";
    protected const string BaseUrl = "https://huggingface.co/PALs-Software/DigitalAssistant.AsrModels/resolve/main";

    public string GetCompleteDownloadLinkForModel(AsrModels model, AsrMode mode, AsrPrecision precision, bool dataModel = false)
    {
        if (dataModel)
            return $"{BaseUrl}/{model}/{mode}/{precision}/model_with_beam_search.onnx.data{DownloadParameter}";
        else
            return $"{BaseUrl}/{model}/{mode}/{precision}/whisper_{model.ToString().ToLower()}_{mode.ToString().ToLower()}_{precision.ToString().ToLower()}.onnx{DownloadParameter}";
    }
}
