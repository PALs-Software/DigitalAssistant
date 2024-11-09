using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Models;
using DigitalAssistant.Abstractions.Dashboards.Interfaces;
using DigitalAssistant.Abstractions.Devices.Enums;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Modules.Connectors.Services;
using DigitalAssistant.Server.Modules.Groups.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalAssistant.Server.Modules.Devices.Models;

[Index(nameof(Type))]
public partial class Device : BaseModel, IDevice, IDashboardEntry
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

    [Visible(DisplayOrder = 500, HideInGUITypes = [GUIType.ListPart])]
    [ForeignKey(nameof(Group))]
    public virtual Guid? GroupId { get; set; } = null;
    public virtual Group? Group { get; set; } = null;

    [Visible(DisplayOrder = 600)]
    public bool ShowInDashboard { get; set; } = true;

    [Visible(DisplayOrder = 700)]
    public int DashboardOrder { get; set; }

    [Required]
    [Editable(false)]
    [Visible(DisplayOrder = 800)]
    public string Connector { get; set; } = null!;

    [Editable(false)]
    [Visible(DisplayOrder = 900)]
    public string? Manufacturer { get; set; }

    [Editable(false)]
    [Visible(DisplayOrder = 1000)]
    public string? ProductName { get; set; }

    public string? AdditionalJsonData { get; set; }

    [Editable(false)]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayGroup = "Information", DisplayGroupOrder = 9999, DisplayOrder = 100, Collapsed = true, HideInGUITypes = [GUIType.List])]
    public override DateTime CreatedOn { get => base.CreatedOn; set => base.CreatedOn = value; }

    [Editable(false)]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayGroup = "Information", DisplayOrder = 200, HideInGUITypes = [GUIType.List])]
    public override DateTime ModifiedOn { get => base.ModifiedOn; set => base.ModifiedOn = value; }

    #region Not Mapped
    [NotMapped] private bool NameChanged = false;
    [NotMapped] protected List<string> ChangedProperties = [];
    #endregion

    #endregion

    #region CRUD
    public override Task OnAfterPropertyChanged(OnAfterPropertyChangedArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(Name):
                CustomName = true;
                NameChanged = true;
                break;
        }

        if (args.OldValue != args.NewValue && !ChangedProperties.Contains(args.PropertyName))
            ChangedProperties.Add(args.PropertyName);

        return base.OnAfterPropertyChanged(args);
    }

    public override Task OnAfterListPropertyChanged(OnAfterListPropertyChangedArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(AlternativeNames):
                NameChanged = true;
                break;
        }

        return base.OnAfterListPropertyChanged(args);
    }

    public override Task OnAfterRemoveListEntry(OnAfterRemoveListEntryArgs args)
    {
        NameChanged = true;
        return base.OnAfterRemoveListEntry(args);
    }

    public override async Task OnAfterCardSaveChanges(OnAfterCardSaveChangesArgs args)
    {
        await base.OnAfterCardSaveChanges(args);

        if (NameChanged)
        {
            var commandHandler = args.EventServices.ServiceProvider.GetRequiredService<CommandHandler>();
            await commandHandler.RefreshLocalizedCommandTemplatesCacheAsync(clearAllLanguages: true).ConfigureAwait(false);
            NameChanged = false;
        }

        if (ChangedProperties.Count == 0 || !await ActionRelevantPropertiesChangedAsync())
            return;

        var connectorService = args.EventServices.ServiceProvider.GetRequiredService<ConnectorService>();
        var actionArgs = await CreateActionArgsAsync();
        var result = await connectorService.ExecuteDeviceActionAsync(this, actionArgs);
        if (!result.Success)
            throw new CRUDException(result.ErrorMessage ?? args.EventServices.Localizer["UnkownErrorMessage"]);

        ChangedProperties.Clear();
    }
    #endregion

    #region Actions

    protected virtual Task<bool> ActionRelevantPropertiesChangedAsync()
    {
        return Task.FromResult(false);
    }

    protected virtual Task<IDeviceActionArgs> CreateActionArgsAsync()
    {
        throw new NotImplementedException("Must be implemented in the derived class");
    }

    #endregion
}
