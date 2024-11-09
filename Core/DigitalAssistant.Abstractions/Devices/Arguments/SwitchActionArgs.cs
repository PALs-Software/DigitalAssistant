using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Devices.Arguments;

public class SwitchActionArgs : IDeviceActionArgs
{
    public bool? On { get; set; }
}