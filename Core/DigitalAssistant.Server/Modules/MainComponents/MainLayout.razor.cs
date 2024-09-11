using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.User.Models;
using DigitalAssistant.Server.Modules.CacheModule;
using DigitalAssistant.Server.Modules.Clients.Components;
using DigitalAssistant.Server.Modules.Menus.Components;
using DigitalAssistant.Server.Modules.Setups.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.MainComponents;

public partial class MainLayout
{
    #region Injects
    [Inject] protected IStringLocalizer<MainLayout> Localizer { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected IBlazorBaseUserOptions Options { get; set; } = null!;

    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected ClientStatusPopup ClientStatusPopup { get; set; } = null!;
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = null!;
    #endregion

    #region Members
    protected ThemeSelector? ThemeSelector;
    protected bool InitialSetupCompleted { get; set; }
    #endregion
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (Cache.SetupCache.Setup == null || !Cache.SetupCache.Setup.InitalSetupCompleted)
            InitialSetupCompleted = await Setup.InitialSetupCompletedAsync(DbContext);
        else
            InitialSetupCompleted = true;
    }
  
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _ = InvokeAsync(StateHasChanged);

        if (!InitialSetupCompleted)
            return;

        var authState = await AuthenticationState;
        if (authState.User.Identity?.IsAuthenticated ?? false)
            return;

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        NavigationManager.NavigateTo($"{Options.LoginPath}?returnUrl={uri.PathAndQuery}", true);
    }    
}
