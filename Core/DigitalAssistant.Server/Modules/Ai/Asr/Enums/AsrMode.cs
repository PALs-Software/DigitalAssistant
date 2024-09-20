namespace DigitalAssistant.Server.Modules.Ai.Asr.Enums;

public enum AsrMode
{
    Cpu,
#if GPUSUPPORTENABLED
    Gpu
#endif
}
