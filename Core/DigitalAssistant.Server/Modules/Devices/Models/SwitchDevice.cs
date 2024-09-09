using BlazorBase.Abstractions.CRUD.Attributes;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Server.Modules.Devices.Models;

public class SwitchDevice : Device, ISwitchDevice
{
    [Visible(DisplayGroup = "Switch Settings", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public bool On { get; set; }
}
