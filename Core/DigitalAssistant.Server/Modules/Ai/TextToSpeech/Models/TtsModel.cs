using DigitalAssistant.Server.Modules.Ai.TextToSpeech.Enums;

namespace DigitalAssistant.Server.Modules.Ai.TextToSpeech.Models;

public record TtsModel(string Name, TtsModelQuality Quality, string Link)
{
}
