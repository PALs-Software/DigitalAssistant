using DigitalAssistant.Server.Modules.Users;
using BlazorBase.CRUD.Models;
using BlazorBase.DataUpgrade;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using DigitalAssistant.Server.Modules.Commands.Components;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class SideMenu : ComponentBase
{
    #region Injects
    [Inject] protected IStringLocalizer<SideMenu> Localizer { get; set; } = null!;
    [Inject] protected IStringLocalizer<DataUpgradeEntry> DataUpgradeEntryLocalizer { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = null!;
    #endregion

    #region Member
    protected List<NavigationEntry> AdministrationNavigationEntries = [];
    protected List<NavigationEntry> GeneralNavigationEntries = [];

    protected string? DashboardLink = "/Dashboard";

    private bool Visible = true;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationState;
        var isAdmin = authState.User.IsInRole(UserRole.Admin.ToString());

        if (isAdmin)
        {
            AdministrationNavigationEntries.AddRange([
                new(Localizer[$"Setup"], "Setup", FontAwesomeIcons.Wrench),
                new(Localizer[$"Clients"], "Clients", "fa-television"),
                new(Localizer[$"Files"], "ServerFiles", FontAwesomeIcons.FolderOpen),
                new(Localizer[$"Users"], "Users", FontAwesomeIcons.Users),
                new(Localizer[$"Connectors"], "Connectors", FontAwesomeIcons.Link),
                new(Localizer[$"Commands"], "Commands", "fa-book-journal-whills"),
            ]);
        }

        GeneralNavigationEntries.AddRange([
            new(Localizer[$"Devices"], "Devices", FontAwesomeIcons.Microchip),
        ]);
    }

    private void ToogleVisibility()
    {
        Visible = !Visible;
    }
}