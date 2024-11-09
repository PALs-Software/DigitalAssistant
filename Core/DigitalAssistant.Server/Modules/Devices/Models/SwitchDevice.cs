using BlazorBase.Abstractions.CRUD.Attributes;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Server.Modules.Devices.Models;

public class SwitchDevice : Device, ISwitchDevice
{
    [Visible(DisplayGroup = "Switch Settings", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public bool On { get; set; }

    #region Actions
    protected override Task<bool> ActionRelevantPropertiesChangedAsync()
    {
        return Task.FromResult(ChangedProperties.Count != 0 && ChangedProperties.Contains(nameof(On)));
    }

    protected override Task<IDeviceActionArgs> CreateActionArgsAsync()
    {
        return Task.FromResult((IDeviceActionArgs)new SwitchActionArgs()
        {
            On = ChangedProperties.Contains(nameof(On)) ? On : null,
        });
    }
    #endregion

}
