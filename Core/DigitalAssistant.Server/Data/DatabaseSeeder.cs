using DigitalAssistant.Server.Modules.Users;

namespace DigitalAssistant.Server.Data;

public static class DatabaseSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        await UserService.SeedUserRolesAsync(serviceProvider);
    }
}
