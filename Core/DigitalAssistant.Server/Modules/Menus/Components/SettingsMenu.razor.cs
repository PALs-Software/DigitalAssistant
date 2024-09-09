using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class SettingsMenu
{
    #region Parameter
    [CascadingParameter] public ThemeSelector? ThemeSelector { get; set; } = null;
    #endregion

    #region Injects
    [Inject] protected IStringLocalizer<SettingsMenu> Localizer { get; set; } = null!;
    [Inject] protected UserService UserService { get; set; } = null!;
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    #endregion

    #region Members
    protected bool UserPrefersDarkMode = false;
    protected Dictionary<string, object> InputAttributes = [];
    #endregion

    protected override async Task OnInitializedAsync()
    {
        var currentUser = await UserService.GetCurrentUserAsync(DbContext, asNoTracking: false);
        if (currentUser == null || currentUser.PrefersDarkMode == null)
            UserPrefersDarkMode = ThemeSelector?.GetUserPrefersDarkMode() ?? false;
        else
            UserPrefersDarkMode = currentUser.PrefersDarkMode.Value;

        if (UserPrefersDarkMode)
            InputAttributes.Add("checked", "");
    }

    protected async Task ChangeDarkModeSettingAsync(ChangeEventArgs args)
    {
        InputAttributes.Clear();
        if (args.Value is not bool userPrefersDarkMode)
            return;

        UserPrefersDarkMode = userPrefersDarkMode;
        var currentUser = await UserService.GetCurrentUserAsync(DbContext, asNoTracking: false);
        if (currentUser != null)
        {
            currentUser.PrefersDarkMode = UserPrefersDarkMode;
            await DbContext.SaveChangesAsync();
        }

        ThemeSelector?.SetUserPrefersDarkMode(UserPrefersDarkMode);
    }
}
