
using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Abstractions.Dashboards.Interfaces;
using DigitalAssistant.Abstractions.Devices.Arguments;
using DigitalAssistant.Abstractions.Devices.Interfaces;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Connectors.Services;
using DigitalAssistant.Server.Modules.Devices.Models;
using DigitalAssistant.Server.Modules.Groups.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Dashboards;

public partial class Dashboard
{

    #region Injects
    [Inject] protected IStringLocalizer<Dashboard> Localizer { get; set; } = null!;
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected ConnectorService ConnectorService { get; set; } = null!;
    #endregion

    #region Members
    protected record DashboardGroup(string Name, string? IconLink, List<IDashboardEntry> Entries);

    protected List<DashboardGroup> DashboardGroups = [];
    #endregion

    protected override async Task OnInitializedAsync()
    {
        var groups = await DbContext.SetAsync((IQueryable<Group> query) => query
            .Where(entry => entry.ShowInDashboard)
            .Include(entry => entry.Devices)
            .Include(entry => entry.Clients)
            .Include(entry => entry.Icon)
            .OrderBy(entry => entry.DashboardOrder)
            .AsNoTracking()
            .ToList()
        );

        groups.Add(new Group()
        {
            Name = Localizer["Ungrouped"],
            Devices = await DbContext.SetAsync((IQueryable<Device> query) => query
                .Where(entry => entry.GroupId == null && entry.ShowInDashboard)
                .AsNoTracking()
                .OrderBy(entry => entry.DashboardOrder)
                .ToList()
            ),
            Clients = await DbContext.SetAsync((IQueryable<Client> query) => query
               .Where(entry => entry.GroupId == null && entry.ShowInDashboard)
               .AsNoTracking()
               .OrderBy(entry => entry.DashboardOrder)
               .ToList()
            )
        });

        foreach (var group in groups)
        {
            var entries = new List<IDashboardEntry>(group.Devices);
            entries.AddRange(group.Clients);

            if (entries.Count != 0)
                DashboardGroups.Add(new DashboardGroup(group.Name, group.Icon?.GetFileLink(), entries.OrderBy(entry => entry.DashboardOrder).ToList()));
        }
    }

    protected async Task ChangeLightStatusAsync(ILightDevice lightDevice, ChangeEventArgs args)
    {
        if (args.Value is bool boolValue)
            await ConnectorService.ExecuteDeviceActionAsync(lightDevice, new LightActionArgs { On = boolValue });
    }
}
