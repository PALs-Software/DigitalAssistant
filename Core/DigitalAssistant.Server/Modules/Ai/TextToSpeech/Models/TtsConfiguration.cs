using System.Text.Json.Serialization;

namespace DigitalAssistant.Server.Modules.Ai.TextToSpeech.Models;

public class TtsConfiguration
{
    [JsonPropertyName("audio")]
    public TtsAudioConfiguration Audio { get; set; } = new();
    [JsonPropertyName("espeak")]
    public TtsEspeakConfiguration Espeak { get; set; } = new();

    [JsonPropertyName("phoneme_id_map")]
    public Dictionary<char, long[]> PhonemeMapping { get; set; } = [];
}

public class TtsAudioConfiguration
{
    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; }
}

public class TtsEspeakConfiguration
{
    [JsonPropertyName("voice")]
    public string? Voice { get; set; }
}