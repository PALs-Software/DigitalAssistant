using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.CRUD.Attributes;
using BlazorBase.CRUD.Models;
using BlazorBase.Files.Attributes;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Abstractions.Groups.Interfaces;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Groups.Models;

[Route("/Groups")]
[Authorize(Roles = "Admin, User")]
public class Group : BaseModel, IGroup
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [DisplayKey]
    [StringLength(256)]
    [Visible(DisplayOrder = 100)]
    public string Name { get; set; } = String.Empty;

    [FileInputFilter(Filter = "image/*")]
    [MaxFileSize(MaxFileSize = 10485760)] // 10 MB
    [Visible(DisplayOrder = 200)]
    public virtual ServerFile? Icon { get; set; }

    [Visible(DisplayOrder = 300)]
    public bool ShowInDashboard { get; set; } = true;

    [Visible(DisplayOrder = 400)]
    public int DashboardOrder { get; set; }

    [DataType(DataType.Html)]
    [Visible(DisplayGroup = "Description", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public string? Description { get; set; }

    [Visible(DisplayGroup = "Alternative Names", DisplayGroupOrder = 200, DisplayOrder = 100, HideInGUITypes = [GUIType.List])]
    public List<string> AlternativeNames { get; set; } = [];

    [BaseListPartDisplayOptions(ShowAddButton = false)]
    [Visible(DisplayGroup = "Devices", DisplayGroupOrder = 300, DisplayOrder = 100)]
    public virtual List<Device> Devices { get; set; } = [];

    [BaseListPartDisplayOptions(ShowAddButton = false)]
    [Visible(DisplayGroup = "Clients", DisplayGroupOrder = 400, DisplayOrder = 100)]
    public virtual List<Client> Clients { get; set; } = [];

    List<IDevice> IGroup.Devices => Devices.Cast<IDevice>().ToList();
    List<IClient> IGroup.Clients => Clients.Cast<IClient>().ToList();
}
