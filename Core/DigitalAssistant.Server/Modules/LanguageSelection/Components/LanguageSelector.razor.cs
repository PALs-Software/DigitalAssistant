using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.LanguageSelection.Components;

public partial class LanguageSelector
{
    #region Injects
    [Inject] protected IStringLocalizer<LanguageSelector> Localizer { get; set; } = null!;
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected UserService UserService { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    #endregion

    #region Members
    public static string[] SupportedCultures =
    [
        "en-US",
        "de-DE"
    ];
    #endregion

    protected async Task SelectCultureAsync(string cultureName)
    {
        var newCultureInfo = new CultureInfo(cultureName);
        if (newCultureInfo.Name == CultureInfo.CurrentCulture.Name)
            return;

        var currentUser = await UserService.GetCurrentUserAsync(DbContext, asNoTracking: false);
        if (currentUser != null)
        {
            currentUser.PreferredCulture = newCultureInfo.LCID;
            await DbContext.SaveChangesAsync();
        }

        var uri = new Uri(NavigationManager.Uri).GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
        var cultureEscaped = Uri.EscapeDataString(newCultureInfo.Name);
        var uriEscaped = Uri.EscapeDataString(uri);

        NavigationManager.NavigateTo($"Language/Set?culture={cultureEscaped}&redirectUri={uriEscaped}", forceLoad: true);
    }
}
