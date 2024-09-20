using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Enums;
using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Models;

namespace DigitalAssistant.Server.Modules.Ai.TextToSpeech.Services;

public class TtsModelSelectionService
{
    protected const string DownloadParameter = "?download=true";
    protected const string BaseUrl = "https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.0/";

    public List<TtsModel> GetModelsForLanguage(TtsLanguages language)
    {
        return TtsModels[language];
    }

    public TtsModel? GetModel(TtsLanguages language, string name, TtsModelQuality quality)
    {
        var models = TtsModels[language];
        return models.Where(entry => entry.Name == name && entry.Quality == quality).FirstOrDefault();
    }

    public string GetCompleteDownloadLinkForModel(TtsModel model, bool jsonFile = false)
    {
        return BaseUrl + model.Link + (jsonFile ? ".json" : "") + DownloadParameter;
    }

    protected Dictionary<TtsLanguages, List<TtsModel>> TtsModels = new()
    {
        [TtsLanguages.Arabic] = [
            new TtsModel("Kareem", TtsModelQuality.Low, "ar/ar_JO/kareem/low/ar_JO-kareem-low.onnx"),
            new TtsModel("Kareem", TtsModelQuality.Medium, "ar/ar_JO/kareem/medium/ar_JO-kareem-medium.onnx")
        ],
        [TtsLanguages.Catalan] = [
            new TtsModel("Upc_ona", TtsModelQuality.XLow, "ca/ca_ES/upc_ona/x_low/ca_ES-upc_ona-x_low.onnx"),
            new TtsModel("Upc_ona", TtsModelQuality.Medium, "ca/ca_ES/upc_ona/medium/ca_ES-upc_ona-medium.onnx"),
            new TtsModel("Upc_pau", TtsModelQuality.XLow, "ca/ca_ES/upc_pau/x_low/ca_ES-upc_pau-x_low.onnx")
        ],
        [TtsLanguages.Chinese] = [
            new TtsModel("Huayan", TtsModelQuality.XLow, "zh/zh_CN/huayan/x_low/zh_CN-huayan-x_low.onnx"),
            new TtsModel("Huayan", TtsModelQuality.Medium, "zh/zh_CN/huayan/medium/zh_CN-huayan-medium.onnx"),
        ],
        [TtsLanguages.Czech] = [
            new TtsModel("Jirka", TtsModelQuality.Low, "cs/cs_CZ/jirka/low/cs_CZ-jirka-low.onnx"),
            new TtsModel("Jirka", TtsModelQuality.Medium, "cs/cs_CZ/jirka/medium/cs_CZ-jirka-medium.onnx")
        ],
        [TtsLanguages.Danish] = [
            new TtsModel("Talesyntese", TtsModelQuality.Medium, "da/da_DK/talesyntese/medium/da_DK-talesyntese-medium.onnx")
        ],
        [TtsLanguages.Dutch] = [
            new TtsModel("Mls", TtsModelQuality.Medium, "nl/nl_NL/mls/medium/nl_NL-mls-medium.onnx"),
            new TtsModel("Mls_5809", TtsModelQuality.Low, "nl/nl_NL/mls_5809/low/nl_NL-mls_5809-low.onnx"),
            new TtsModel("Mls_7432", TtsModelQuality.Low, "nl/nl_NL/mls_7432/low/nl_NL-mls_7432-low.onnx")
        ],
        [TtsLanguages.Dutch_Belgian] = [
            new TtsModel("Nathalie", TtsModelQuality.XLow, "nl/nl_BE/nathalie/x_low/nl_BE-nathalie-x_low.onnx"),
            new TtsModel("Nathalie", TtsModelQuality.Medium, "nl/nl_BE/nathalie/medium/nl_BE-nathalie-medium.onnx"),
            new TtsModel("Rdh", TtsModelQuality.XLow, "nl/nl_BE/rdh/x_low/nl_BE-rdh-x_low.onnx"),
            new TtsModel("Rdh", TtsModelQuality.Medium, "nl/nl_BE/rdh/medium/nl_BE-rdh-medium.onnx")
        ],
        [TtsLanguages.English_GB] = [
            new TtsModel("Alan", TtsModelQuality.Low, "en/en_GB/alan/low/en_GB-alan-low.onnx"),
            new TtsModel("Alan", TtsModelQuality.Medium, "en/en_GB/alan/medium/en_GB-alan-medium.onnx"),
            new TtsModel("Alba", TtsModelQuality.Medium, "en/en_GB/alba/medium/en_GB-alba-medium.onnx"),
            new TtsModel("Aru", TtsModelQuality.Medium, "en/en_GB/aru/medium/en_GB-aru-medium.onnx"),
            new TtsModel("Cori", TtsModelQuality.Medium, "en/en_GB/cori/medium/en_GB-cori-medium.onnx"),
            new TtsModel("Cori", TtsModelQuality.High, "en/en_GB/cori/high/en_GB-cori-high.onnx"),
            new TtsModel("Jenny_dioco", TtsModelQuality.Medium, "en/en_GB/jenny_dioco/medium/en_GB-jenny_dioco-medium.onnx"),
            new TtsModel("Northern_english_male", TtsModelQuality.Medium, "en/en_GB/northern_english_male/medium/en_GB-northern_english_male-medium.onnx"),
            new TtsModel("Semaine", TtsModelQuality.Medium, "en/en_GB/semaine/medium/en_GB-semaine-medium.onnx"),
            new TtsModel("Southern_english_female", TtsModelQuality.Low, "en/en_GB/southern_english_female/low/en_GB-southern_english_female-low.onnx"),
            new TtsModel("Vctk", TtsModelQuality.Medium, "en/en_GB/vctk/medium/en_GB-vctk-medium.onnx")
        ],
        [TtsLanguages.English_US] = [
            new TtsModel("Amy", TtsModelQuality.Low, "en/en_US/amy/low/en_US-amy-low.onnx"),
            new TtsModel("Amy", TtsModelQuality.Medium, "en/en_US/amy/medium/en_US-amy-medium.onnx"),
            new TtsModel("Arctic", TtsModelQuality.Medium, "en/en_US/arctic/medium/en_US-arctic-medium.onnx"),
            new TtsModel("Bryce", TtsModelQuality.Medium, "en/en_US/bryce/medium/en_US-bryce-medium.onnx"),
            new TtsModel("Danny", TtsModelQuality.Low, "en/en_US/danny/low/en_US-danny-low.onnx"),
            new TtsModel("Hfc_female", TtsModelQuality.Medium, "en/en_US/hfc_female/medium/en_US-hfc_female-medium.onnx"),
            new TtsModel("Hfc_male", TtsModelQuality.Medium, "en/en_US/hfc_male/medium/en_US-hfc_male-medium.onnx"),
            new TtsModel("Joe", TtsModelQuality.Medium, "en/en_US/joe/medium/en_US-joe-medium.onnx"),
            new TtsModel("John", TtsModelQuality.Medium, "en/en_US/john/medium/en_US-john-medium.onnx"),
            new TtsModel("Kathleen", TtsModelQuality.Low, "en/en_US/kathleen/low/en_US-kathleen-low.onnx"),
            new TtsModel("Kristin", TtsModelQuality.Medium, "en/en_US/kristin/medium/en_US-kristin-medium.onnx"),
            new TtsModel("Kusal", TtsModelQuality.Medium, "en/en_US/kusal/medium/en_US-kusal-medium.onnx"),
            new TtsModel("L2arctic", TtsModelQuality.Medium, "en/en_US/l2arctic/medium/en_US-l2arctic-medium.onnx"),
            new TtsModel("Lessac", TtsModelQuality.Low, "en/en_US/lessac/low/en_US-lessac-low.onnx"),
            new TtsModel("Lessac", TtsModelQuality.Medium, "en/en_US/lessac/medium/en_US-lessac-medium.onnx"),
            new TtsModel("Lessac", TtsModelQuality.High, "en/en_US/lessac/high/en_US-lessac-high.onnx"),
            new TtsModel("Libritts", TtsModelQuality.High, "en/en_US/libritts/high/en_US-libritts-high.onnx"),
            new TtsModel("Libritts_r", TtsModelQuality.Medium, "en/en_US/libritts_r/medium/en_US-libritts_r-medium.onnx"),
            new TtsModel("Ljspeech", TtsModelQuality.Medium, "en/en_US/ljspeech/medium/en_US-ljspeech-medium.onnx"),
            new TtsModel("Ljspeech", TtsModelQuality.High, "en/en_US/ljspeech/high/en_US-ljspeech-high.onnx"),
            new TtsModel("Norman", TtsModelQuality.Medium, "en/en_US/norman/medium/en_US-norman-medium.onnx"),
            new TtsModel("Ryan", TtsModelQuality.Low, "en/en_US/ryan/low/en_US-ryan-low.onnx"),
            new TtsModel("Ryan", TtsModelQuality.Medium, "en/en_US/ryan/medium/en_US-ryan-medium.onnx"),
            new TtsModel("Ryan", TtsModelQuality.High, "en/en_US/ryan/high/en_US-ryan-high.onnx")
        ],
        [TtsLanguages.Farsi] = [
            new TtsModel("Amir", TtsModelQuality.Medium, "fa/fa_IR/amir/medium/fa_IR-amir-medium.onnx"),
            new TtsModel("Gyro", TtsModelQuality.Medium, "fa/fa_IR/gyro/medium/fa_IR-gyro-medium.onnx")
        ],
        [TtsLanguages.Finnish] = [
            new TtsModel("Harri", TtsModelQuality.Low, "fi/fi_FI/harri/low/fi_FI-harri-low.onnx"),
            new TtsModel("Harri", TtsModelQuality.Medium, "fi/fi_FI/harri/medium/fi_FI-harri-medium.onnx")
        ],
        [TtsLanguages.French] = [
            new TtsModel("Gilles", TtsModelQuality.Low, "fr/fr_FR/gilles/low/fr_FR-gilles-low.onnx"),
            new TtsModel("Mls", TtsModelQuality.Medium, "fr/fr_FR/mls/medium/fr_FR-mls-medium.onnx"),
            new TtsModel("Mls_1840", TtsModelQuality.Low, "fr/fr_FR/mls_1840/low/fr_FR-mls_1840-low.onnx"),
            new TtsModel("Siwis", TtsModelQuality.Low, "fr/fr_FR/siwis/low/fr_FR-siwis-low.onnx"),
            new TtsModel("Siwis", TtsModelQuality.Medium, "fr/fr_FR/siwis/medium/fr_FR-siwis-medium.onnx"),
            new TtsModel("Tom", TtsModelQuality.Medium, "fr/fr_FR/tom/medium/fr_FR-tom-medium.onnx"),
            new TtsModel("Upmc", TtsModelQuality.Medium, "fr/fr_FR/upmc/medium/fr_FR-upmc-medium.onnx")
        ],
        [TtsLanguages.Georgian] = [
            new TtsModel("Natia", TtsModelQuality.Medium, "ka/ka_GE/natia/medium/ka_GE-natia-medium.onnx")
        ],
        [TtsLanguages.German] = [
            new TtsModel("Eva_k", TtsModelQuality.XLow, "de/de_DE/eva_k/x_low/de_DE-eva_k-x_low.onnx"),
            new TtsModel("Karlsson", TtsModelQuality.Low, "de/de_DE/karlsson/low/de_DE-karlsson-low.onnx"),
            new TtsModel("Kerstin", TtsModelQuality.Low, "de/de_DE/kerstin/low/de_DE-kerstin-low.onnx"),
            new TtsModel("Mls", TtsModelQuality.Medium, "de/de_DE/mls/medium/de_DE-mls-medium.onnx"),
            new TtsModel("Pavoque", TtsModelQuality.Low, "de/de_DE/pavoque/low/de_DE-pavoque-low.onnx"),
            new TtsModel("Ramona", TtsModelQuality.Low, "de/de_DE/ramona/low/de_DE-ramona-low.onnx"),
            new TtsModel("Thorsten", TtsModelQuality.Low, "de/de_DE/thorsten/low/de_DE-thorsten-low.onnx"),
            new TtsModel("Thorsten", TtsModelQuality.Medium, "de/de_DE/thorsten/medium/de_DE-thorsten-medium.onnx"),
            new TtsModel("Thorsten", TtsModelQuality.High, "de/de_DE/thorsten/high/de_DE-thorsten-high.onnx"),
            new TtsModel("Thorsten_emotional", TtsModelQuality.Medium, "de/de_DE/thorsten_emotional/medium/de_DE-thorsten_emotional-medium.onnx")
        ],
        [TtsLanguages.Greek] = [
            new TtsModel("Rapunzelina", TtsModelQuality.Low, "el/el_GR/rapunzelina/low/el_GR-rapunzelina-low.onnx")
        ],
        [TtsLanguages.Hungarian] = [
            new TtsModel("Anna", TtsModelQuality.Medium, "hu/hu_HU/anna/medium/hu_HU-anna-medium.onnx"),
            new TtsModel("Berta", TtsModelQuality.Medium, "hu/hu_HU/berta/medium/hu_HU-berta-medium.onnx"),
            new TtsModel("Imre", TtsModelQuality.Medium, "hu/hu_HU/imre/medium/hu_HU-imre-medium.onnx")
        ],
        [TtsLanguages.Icelandic] = [
            new TtsModel("Bui", TtsModelQuality.Medium, "is/is_IS/bui/medium/is_IS-bui-medium.onnx"),
            new TtsModel("Salka", TtsModelQuality.Medium, "is/is_IS/salka/medium/is_IS-salka-medium.onnx"),
            new TtsModel("Steinn", TtsModelQuality.Medium, "is/is_IS/steinn/medium/is_IS-steinn-medium.onnx"),
            new TtsModel("Ugla", TtsModelQuality.Medium, "is/is_IS/ugla/medium/is_IS-ugla-medium.onnx")
        ],
        [TtsLanguages.Italian] = [
            new TtsModel("Paola", TtsModelQuality.Medium, "it/it_IT/paola/medium/it_IT-paola-medium.onnx"),
            new TtsModel("Riccardo", TtsModelQuality.XLow, "it/it_IT/riccardo/x_low/it_IT-riccardo-x_low.onnx")
        ],
        [TtsLanguages.Kazakh] = [
            new TtsModel("Iseke", TtsModelQuality.XLow, "kk/kk_KZ/iseke/x_low/kk_KZ-iseke-x_low.onnx"),
            new TtsModel("Issai", TtsModelQuality.High, "kk/kk_KZ/issai/high/kk_KZ-issai-high.onnx"),
            new TtsModel("Raya", TtsModelQuality.XLow, "kk/kk_KZ/raya/x_low/kk_KZ-raya-x_low.onnx")
        ],
        [TtsLanguages.Luxembourgish] = [
            new TtsModel("Marylux", TtsModelQuality.Medium, "lb/lb_LU/marylux/medium/lb_LU-marylux-medium.onnx")
        ],
        [TtsLanguages.Nepali] = [
            new TtsModel("Google", TtsModelQuality.XLow, "ne/ne_NP/google/x_low/ne_NP-google-x_low.onnx"),
            new TtsModel("Google", TtsModelQuality.Medium, "ne/ne_NP/google/medium/ne_NP-google-medium.onnx")
        ],
        [TtsLanguages.Norwegian] = [
            new TtsModel("Talesyntese", TtsModelQuality.Medium, "no/no_NO/talesyntese/medium/no_NO-talesyntese-medium.onnx")
        ],
        [TtsLanguages.Polish] = [
            new TtsModel("Darkman", TtsModelQuality.Medium, "pl/pl_PL/darkman/medium/pl_PL-darkman-medium.onnx"),
            new TtsModel("Gosia", TtsModelQuality.Medium, "pl/pl_PL/gosia/medium/pl_PL-gosia-medium.onnx"),
            new TtsModel("Mc_speech", TtsModelQuality.Medium, "pl/pl_PL/mc_speech/medium/pl_PL-mc_speech-medium.onnx"),
            new TtsModel("Mls_6892", TtsModelQuality.Low, "pl/pl_PL/mls_6892/low/pl_PL-mls_6892-low.onnx")
        ],
        [TtsLanguages.Portuguese] = [
            new TtsModel("Tugão", TtsModelQuality.Medium, "pt/pt_PT/tugão/medium/pt_PT-tugão-medium.onnx")
        ],
        [TtsLanguages.Portuguese_Brazilian] = [
            new TtsModel("Edresson", TtsModelQuality.Low, "pt/pt_BR/edresson/low/pt_BR-edresson-low.onnx"),
            new TtsModel("Faber", TtsModelQuality.Medium, "pt/pt_BR/faber/medium/pt_BR-faber-medium.onnx")
        ],
        [TtsLanguages.Romanian] = [
            new TtsModel("Mihai", TtsModelQuality.Medium, "ro/ro_RO/mihai/medium/ro_RO-mihai-medium.onnx")
        ],
        [TtsLanguages.Russian] = [
            new TtsModel("Denis", TtsModelQuality.Medium, "ru/ru_RU/denis/medium/ru_RU-denis-medium.onnx"),
            new TtsModel("Dmitri", TtsModelQuality.Medium, "ru/ru_RU/dmitri/medium/ru_RU-dmitri-medium.onnx"),
            new TtsModel("Irina", TtsModelQuality.Medium, "ru/ru_RU/irina/medium/ru_RU-irina-medium.onnx"),
            new TtsModel("Ruslan", TtsModelQuality.Medium, "ru/ru_RU/ruslan/medium/ru_RU-ruslan-medium.onnx")
        ],
        [TtsLanguages.Serbian] = [
            new TtsModel("Serbski_institut", TtsModelQuality.Medium, "sr/sr_RS/serbski_institut/medium/sr_RS-serbski_institut-medium.onnx")
        ],
        [TtsLanguages.Slovak] = [
            new TtsModel("Lili", TtsModelQuality.Medium, "sk/sk_SK/lili/medium/sk_SK-lili-medium.onnx")
        ],
        [TtsLanguages.Slovenian] = [
            new TtsModel("Artur", TtsModelQuality.Medium, "sl/sl_SI/artur/medium/sl_SI-artur-medium.onnx")
        ],
        [TtsLanguages.Spanish] = [
            new TtsModel("Carlfm", TtsModelQuality.XLow, "es/es_ES/carlfm/x_low/es_ES-carlfm-x_low.onnx"),
            new TtsModel("Davefx", TtsModelQuality.Medium, "es/es_ES/davefx/medium/es_ES-davefx-medium.onnx"),
            new TtsModel("Mls_10246", TtsModelQuality.Low, "es/es_ES/mls_10246/low/es_ES-mls_10246-low.onnx"),
            new TtsModel("Mls_9972", TtsModelQuality.Low, "es/es_ES/mls_9972/low/es_ES-mls_9972-low.onnx"),
            new TtsModel("Sharvard", TtsModelQuality.Medium, "es/es_ES/sharvard/medium/es_ES-sharvard-medium.onnx")
        ],
        [TtsLanguages.Spanish_Mexican] = [
            new TtsModel("Ald", TtsModelQuality.Medium, "es/es_MX/ald/medium/es_MX-ald-medium.onnx"),
            new TtsModel("Claude", TtsModelQuality.High, "es/es_MX/claude/high/es_MX-claude-high.onnx")
        ],
        [TtsLanguages.Swahili] = [
            new TtsModel("Lanfrica", TtsModelQuality.Medium, "sw/sw_CD/lanfrica/medium/sw_CD-lanfrica-medium.onnx")
        ],
        [TtsLanguages.Swedish] = [
            new TtsModel("Nst", TtsModelQuality.Medium, "sv/sv_SE/nst/medium/sv_SE-nst-medium.onnx")
        ],
        [TtsLanguages.Turkish] = [
            new TtsModel("Dfki", TtsModelQuality.Medium, "tr/tr_TR/dfki/medium/tr_TR-dfki-medium.onnx"),
            new TtsModel("Fahrettin", TtsModelQuality.Medium, "tr/tr_TR/fahrettin/medium/tr_TR-fahrettin-medium.onnx"),
            new TtsModel("Fettah", TtsModelQuality.Medium, "tr/tr_TR/fettah/medium/tr_TR-fettah-medium.onnx")
        ],
        [TtsLanguages.Ukrainian] = [
            new TtsModel("Lada", TtsModelQuality.XLow, "uk/uk_UA/lada/x_low/uk_UA-lada-x_low.onnx"),
            new TtsModel("Ukrainian_tts", TtsModelQuality.Medium, "uk/uk_UA/ukrainian_tts/medium/uk_UA-ukrainian_tts-medium.onnx")
        ],
        [TtsLanguages.Vietnamese] = [
            new TtsModel("25hours_single", TtsModelQuality.Low, "vi/vi_VN/25hours_single/low/vi_VN-25hours_single-low.onnx"),
            new TtsModel("Vais1000", TtsModelQuality.Medium, "vi/vi_VN/vais1000/medium/vi_VN-vais1000-medium.onnx"),
            new TtsModel("Vivos", TtsModelQuality.XLow, "vi/vi_VN/vivos/x_low/vi_VN-vivos-x_low.onnx")
        ],
        [TtsLanguages.Welsh] = [
            new TtsModel("Gwryw_gogleddol", TtsModelQuality.Medium, "cy/cy_GB/gwryw_gogleddol/medium/cy_GB-gwryw_gogleddol-medium.onnx")
        ],
    };
}
