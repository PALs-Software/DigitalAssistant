using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.User.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DigitalAssistant.Server.Modules.Users;

public class UserService(AuthenticationStateProvider authenticationStateProvider, UserManager<IdentityUser> userManager, IBaseDbContext dbContext)
    : BaseUserService<User, IdentityUser, UserRole>(authenticationStateProvider, userManager, dbContext)
{
}