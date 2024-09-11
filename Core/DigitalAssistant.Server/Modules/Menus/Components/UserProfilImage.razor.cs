using BlazorBase.Abstractions.CRUD.Interfaces;
using DigitalAssistant.Server.Modules.General;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class UserProfilImage : ComponentBase
{
    #region Injects
    [Inject] protected UserService UserService { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected ScopedEventService ScopedEventService { get; set; } = null!;
    #endregion

    #region Members
    public string? ProfileImageLink = null;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        ScopedEventService.OnProfileImageChanged += ScopedEventService_OnProfileImageChanged;

        await LoadProfileImageAsync();
    }

    private void ScopedEventService_OnProfileImageChanged(object? sender, EventArgs e)
    {
        InvokeAsync(LoadProfileImageAsync);
    }

    protected async Task LoadProfileImageAsync()
    {
        var userId = await UserService.GetCurrentUserIdentityIdAsync();
        if (userId == null)
            return;

        var dbContext = ServiceProvider.GetRequiredService<IBaseDbContext>();
        var user = await dbContext.SetAsync((IQueryable<User> query) =>
        {
            return query
                .Where(user => user.IdentityUserId == userId)
                .Include(user => user.ProfileImage)
                .AsNoTracking()
                .FirstOrDefault();
        });

        ProfileImageLink = user?.ProfileImage?.GetFileLink(useThumbnailIfImage: true);
        _ = InvokeAsync(StateHasChanged);
    }
}