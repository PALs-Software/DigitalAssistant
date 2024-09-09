using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DigitalAssistant.Server.Modules.Users;

[Route("/Users")]
[Authorize(Roles = "Admin")]
public class User : BaseUser<IdentityUser, UserRole>
{
    #region Properties

    [Visible]
    public int PreferredCulture { get; set; }

    [Visible]
    public bool? PrefersDarkMode { get; set; }

    #endregion

    protected override Task<bool> IdentityHasRightToChangeRoleAsync(ClaimsPrincipal currentLoggedInUser, UserRole identityChangedRole, IdentityUser? identityToChange)
    {
        return Task.FromResult(currentLoggedInUser.IsInRole(UserRole.Admin.ToString()));
    }

    public override Task<List<PageActionGroup>?> GeneratePageActionGroupsAsync(EventServices eventServices)
    {
        return Task.FromResult<List<PageActionGroup>?>(null);
    }
}
