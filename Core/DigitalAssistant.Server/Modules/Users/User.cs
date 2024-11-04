using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.Files.Attributes;
using BlazorBase.User.Models;
using DigitalAssistant.Server.Modules.Files;
using DigitalAssistant.Server.Modules.General;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Security.Claims;

namespace DigitalAssistant.Server.Modules.Users;

[Route("/Users")]
[Authorize(Roles = "Admin")]
public partial class User : BaseUser<IdentityUser, UserRole>
{
    #region Properties

    [FileInputFilter(Filter = "image/*")]
    [MaxFileSize(MaxFileSize = 10485760)] // 10 MB
    [Visible(DisplayGroup = "User Settings", DisplayGroupOrder = 100, DisplayOrder = 100)]
    public virtual ServerFile? ProfileImage { get; set; }

    public int PreferredCulture { get; set; }

    [Visible(DisplayGroup = "User Settings", DisplayOrder = 200)]
    public bool? PrefersDarkMode { get; set; }

    #endregion

    protected override Task<bool> IdentityHasRightToChangeRoleAsync(ClaimsPrincipal currentLoggedInUser, UserRole identityChangedRole, IdentityUser? identityToChange)
    {
        return Task.FromResult(currentLoggedInUser.IsInRole(UserRole.Admin.ToString()));
    }

    #region Inital Setup
    [NotMapped] public bool IsCalledFromInitialSetupWizard { get; set; }

    protected override Task CheckIdentityRolePermissionsAsync(EventServices eventServices, UserManager<IdentityUser> userManager, IdentityUser? identityToChange)
    {
        if (IsCalledFromInitialSetupWizard)
            return Task.CompletedTask;

        return base.CheckIdentityRolePermissionsAsync(eventServices, userManager, identityToChange);
    }
    #endregion

    #region CRUD

    public override Task OnAfterCardSaveChanges(OnAfterCardSaveChangesArgs args)
    {
        args.EventServices.ServiceProvider
            .GetRequiredService<ScopedEventService>()
            .InvokeProfileImageChanged();

        return base.OnAfterCardSaveChanges(args);
    }

    public override List<PropertyInfo> GetVisibleProperties(GUIType guiType, List<string> userRoles)
    {
        var properties = base.GetVisibleProperties(guiType, userRoles);

        if (!userRoles.Contains("Admin"))
            properties.RemoveAll(entry => entry.Name == nameof(IdentityRole));

        return properties;
    }
    #endregion

}
