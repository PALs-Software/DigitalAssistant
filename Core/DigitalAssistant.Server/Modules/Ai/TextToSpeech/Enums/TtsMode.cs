namespace DigitalAssistant.Server.Modules.Ai.TextToSpeech.Enums;

public enum TtsMode
{
    Cpu,
#if  GPUSUPPORTENABLED
    Gpu
#endif
}
