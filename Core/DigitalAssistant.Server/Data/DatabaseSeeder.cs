using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.CRUD.Extensions;
using BlazorBase.CRUD.Services;
using BlazorBase.Abstractions.CRUD.Structures;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
#if DEBUG
        if (UserService.DatabaseHasNoUsers(serviceProvider))
        {
            await UserService.SeedUserRolesAsync(serviceProvider);
            await UserService.SeedUserAsync(serviceProvider, "Admin", "admin@mail.com", "oPr#BO|QGupv%Kz&TaL5", UserRole.Admin);
        }      
#endif
    }
}
