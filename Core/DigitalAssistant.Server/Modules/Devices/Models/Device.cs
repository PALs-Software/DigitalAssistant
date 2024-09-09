using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Models;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Devices.Models;

[Index(nameof(Type))]
public partial class Device : BaseModel, IDevice
{
    #region Properties

    [Key]
    public Guid Id { get; set; }

    [Required]
    public string InternalId { get; set; } = null!;

    [Required]
    [DisplayKey]
    [Visible(DisplayOrder = 100)]
    public string Name { get; set; } = null!;

    [Visible(DisplayGroup = "Alternative Names", DisplayGroupOrder = 50, DisplayOrder = 100, HideInGUITypes = [GUIType.List])]
    public List<string> AlternativeNames { get; set; } = [];

    [Visible(DisplayOrder = 200, HideInGUITypes = [GUIType.Card])]
    [CustomSortAndFilterPropertyPath(nameof(AlternativeNames), typeof(string))]
    public string AlternativeNamesFormatted { get => string.Join(", ", AlternativeNames); }

    [Visible(DisplayOrder = 200, HideInGUITypes = [GUIType.List])]
    public bool CustomName { get; set; }

    [Editable(false)]
    [Visible(DisplayOrder = 300)]
    public DeviceType Type { get; set; }

    [Editable(false)]
    [Visible(DisplayOrder = 400)]
    public DeviceStatus Status { get; set; }

    [Required]
    [Editable(false)]
    [Visible(DisplayOrder = 500)]
    public string Connector { get; set; } = null!;

    [Required]
    [Editable(false)]
    [Visible(DisplayOrder = 600)]
    public string Manufacturer { get; set; } = null!;

    [Required]
    [Editable(false)]
    [Visible(DisplayOrder = 700)]
    public string ProductName { get; set; } = null!;

    public string? AdditionalJsonData { get; set; }

    [Editable(false)]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayGroup = "Information", DisplayGroupOrder = 9999, DisplayOrder = 100, Collapsed = true, HideInGUITypes = [GUIType.List])]
    public override DateTime CreatedOn { get => base.CreatedOn; set => base.CreatedOn = value; }

    [Editable(false)]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayGroup = "Information", DisplayOrder = 200, HideInGUITypes = [GUIType.List])]
    public override DateTime ModifiedOn { get => base.ModifiedOn; set => base.ModifiedOn = value; }

    #endregion

    #region CRUD
    public override Task OnAfterPropertyChanged(OnAfterPropertyChangedArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(Name):
                CustomName = true;
                break;
        }

        return base.OnAfterPropertyChanged(args);
    }
    #endregion
}
