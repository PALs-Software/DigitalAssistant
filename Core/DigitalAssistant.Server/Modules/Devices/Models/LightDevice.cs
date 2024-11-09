using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Devices.Models;

public class LightDevice : Device, ILightDevice
{
    #region Properties

    [Visible(DisplayGroup = "Light Settings", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public bool On { get; set; }

    [Editable(false)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 200)]
    public bool IsDimmable { get; set; }

    [Range(0, 100)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 300)]
    public double Brightness { get; set; }

    [Editable(false)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 400)]
    public bool ColorTemperatureIsAdjustable { get; set; }

    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 500)]
    public int? ColorTemperature { get; set; }

    [Editable(false)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 600)]
    public int MinimumColorTemperature { get; set; }

    [Editable(false)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 700)]
    public int MaximumColorTemperature { get; set; }

    [Editable(false)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 800)]
    public bool ColorIsAdjustable { get; set; }

    [PresentationDataType(PresentationDataType.Color)]
    [Visible(DisplayGroup = "Light Settings", DisplayOrder = 900)]
    public string? Color { get; set; }

    #endregion

    #region CRUD

    public override Task OnBeforeValidateProperty(OnBeforeValidatePropertyArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(ColorTemperature):
                if (ColorTemperature < MinimumColorTemperature || ColorTemperature > MaximumColorTemperature)
                {
                    args.ErrorMessage = args.EventServices.Localizer["RangeErrorMessage",
                        args.EventServices.Localizer[nameof(ColorTemperature)],
                        MinimumColorTemperature,
                        MaximumColorTemperature];
                    args.IsValid = false;
                    args.IsHandled = true;
                }
                break;
        }

        return base.OnBeforeValidateProperty(args);
    }
    #endregion

    #region Property Handling

    public override Task OnShowEntry(OnShowEntryArgs args)
    {
        if (!IsDimmable)
        {
            var displayItem = args.VisiblePropertyDictionary.Where(entry => entry.Key.Name == nameof(Brightness)).FirstOrDefault().Value;
            if (displayItem != null)
                displayItem.IsReadOnly = true;
        }

        if (!ColorTemperatureIsAdjustable)
        {
            var displayItem = args.VisiblePropertyDictionary.Where(entry => entry.Key.Name == nameof(ColorTemperature)).FirstOrDefault().Value;
            if (displayItem != null)
                displayItem.IsReadOnly = true;
        }

        if (!ColorIsAdjustable)
        {
            var displayItem = args.VisiblePropertyDictionary.Where(entry => entry.Key.Name == nameof(Color)).FirstOrDefault().Value;
            if (displayItem != null)
                displayItem.IsReadOnly = true;
        }

        return base.OnShowEntry(args);
    }

    #endregion

    #region Actions
    protected override Task<bool> ActionRelevantPropertiesChangedAsync()
    {
        return Task.FromResult(ChangedProperties.Count != 0 && (
            ChangedProperties.Contains(nameof(On)) ||
            ChangedProperties.Contains(nameof(Brightness)) ||
            ChangedProperties.Contains(nameof(ColorTemperature)) ||
            ChangedProperties.Contains(nameof(Color))));
    }

    protected override Task<IDeviceActionArgs> CreateActionArgsAsync()
    {
        return Task.FromResult((IDeviceActionArgs)new LightActionArgs()
        {
            On = ChangedProperties.Contains(nameof(On)) ? On : null,
            Brightness = ChangedProperties.Contains(nameof(Brightness)) ? Brightness : null,
            ColorTemperature = ChangedProperties.Contains(nameof(ColorTemperature)) ? ColorTemperature : null,
            Color = ChangedProperties.Contains(nameof(Color)) ? Color : null,
        });
    }
    #endregion
}
