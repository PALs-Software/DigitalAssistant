using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class ThemeSelector
{
    #region Injects
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected UserService UserService { get; set; } = null!;
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = null!;
    #endregion

    #region Members
    protected bool UserPrefersDarkMode = false;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationState;
        if (!(authState.User.Identity?.IsAuthenticated ?? false))
            return;

        var user = await UserService.GetCurrentUserAsync();
        if (user != null && user.PrefersDarkMode != null)
            UserPrefersDarkMode = user.PrefersDarkMode.Value;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
            return;
        
        var user = await UserService.GetCurrentUserAsync(DbContext, asNoTracking: false);
        if (user == null || user.PrefersDarkMode != null)
            return;

        UserPrefersDarkMode = await JSRuntime.InvokeAsync<bool>("DA.GetUserPrefersDarkMode");
        user.PrefersDarkMode = UserPrefersDarkMode;
        await DbContext.SaveChangesAsync();
        _ = InvokeAsync(StateHasChanged);
    }

    public void SetUserPrefersDarkMode(bool userPrefersDarkMode)
    {
        UserPrefersDarkMode = userPrefersDarkMode;
        InvokeAsync(StateHasChanged);
    }

    public bool GetUserPrefersDarkMode()
    {
        return UserPrefersDarkMode;
    }
}
