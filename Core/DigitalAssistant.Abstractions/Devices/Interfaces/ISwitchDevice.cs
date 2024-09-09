namespace DigitalAssistant.Abstractions.Devices.Interfaces;

public interface ISwitchDevice : IDevice
{
    bool On { get; set; }
}
