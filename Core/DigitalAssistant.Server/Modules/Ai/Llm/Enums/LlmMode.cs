namespace DigitalAssistant.Server.Modules.Ai.Llm.Enums;

public enum LlmMode
{
    Cpu,
#if GPUSUPPORTENABLED
    Gpu
#endif
}
