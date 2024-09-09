using BlazorBase.User.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class UserMenu : ComponentBase
{
    #region Injects
    [Inject] protected IStringLocalizer<UserMenu> Localizer { get; set; } = null!;
    [Inject] protected IBlazorBaseUserOptions Options { get; set; } = null!;
    #endregion

    #region Parameters
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = null!;
    #endregion

    #region Members
    private string UserName = string.Empty;
    private string Greeting = string.Empty;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        if (DateTime.Now < DateTime.Today.AddHours(3) || DateTime.Now > DateTime.Today.AddHours(17))
            Greeting = Localizer["Good evening,"];
        else if (DateTime.Now < DateTime.Today.AddHours(12))
            Greeting = Localizer["Good morning,"];
        else
            Greeting = Localizer["Good afternoon,"];

        var authState = await AuthenticationState;
        if (!authState.User.Identity?.IsAuthenticated ?? false)
            return;

        UserName = authState.User.Identity?.Name ?? String.Empty;
    }
}