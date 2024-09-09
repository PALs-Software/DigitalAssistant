using System.Text.Json.Serialization;

namespace TextToSpeech;

public class ModelConfiguration
{
    [JsonPropertyName("audio")]
    public ModelConfigurationAudio Audio { get; set; } = new();
    [JsonPropertyName("espeak")]
    public ModelConfigurationEspeak Espeak { get; set; } = new();

    [JsonPropertyName("phoneme_id_map")]
    public Dictionary<char, long[]> PhonemeMapping { get; set; } = [];
}

public class ModelConfigurationAudio
{
    [JsonPropertyName("sample_rate")]
    public int SampleRate { get; set; }
}

public class ModelConfigurationEspeak
{
    [JsonPropertyName("voice")]
    public string? Voice { get; set; }
}