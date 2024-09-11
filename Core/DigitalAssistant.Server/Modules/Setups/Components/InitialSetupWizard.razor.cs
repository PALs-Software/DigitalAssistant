using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.CRUD.Components.Card;
using BlazorBase.Modules;
using BlazorBase.User.Models;
using Blazorise;
using DigitalAssistant.Server.Modules.Menus.Components;
using DigitalAssistant.Server.Modules.Setups.Models;
using DigitalAssistant.Server.Modules.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace DigitalAssistant.Server.Modules.Setups.Components;

public partial class InitialSetupWizard : ComponentBase
{
    #region Injects
    [Inject] protected IStringLocalizer<InitialSetupWizard> Localizer { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] protected UserManager<IdentityUser> UserManager { get; set; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected IBlazorBaseUserOptions UserOptions { get; set; } = null!;

    [CascadingParameter] public ThemeSelector? ThemeSelector { get; set; } = null;
    [CascadingParameter] protected Task<AuthenticationState> AuthenticationState { get; set; } = null!;
    #endregion

    #region Members
    protected Carousel? Carousel { get; set; }
    protected BaseCard<User>? UserCard { get; set; }
    protected BaseCard<Setup>? SetupCard { get; set; }
    protected string AdminPassword { get; set; } = String.Empty;

    protected string CreateAdminButtonText = null!;

    protected bool InitialSetupIsCompleted, ShowUserFeedback = false;
    protected MarkupString UserFeedback;

    protected bool ShowGoLeftButton, ShowGoRightButton = false;
    protected Slide MaxAllowedSlide, CurrentSlide = Slide.Admin;
    protected Slide MaxSlide, MinSlide;
    protected enum Slide
    {
        Welcome,
        Admin,
        SetAdminPassword,
        SetupSettings,
        CompleteSetup,
    }
    #endregion

    #region Init

    protected override async Task OnInitializedAsync()
    {
        MaxSlide = (Slide)Enum.GetValues(typeof(Slide)).Cast<int>().Max();
        MinSlide = (Slide)Enum.GetValues(typeof(Slide)).Cast<int>().Min();
        CreateAdminButtonText = Localizer["CreateAdministratorAccount"];

        await SetStatusAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
            return;

        var authState = await AuthenticationState;
        if (authState.User.Identity?.IsAuthenticated ?? false) 
            await JSRuntime.InvokeAsync<bool>("DA.SubmitForm", "logout-form");

        if (await Setup.InitialSetupCompletedAsync(DbContext))
            NavigationManager.NavigateTo("/", true);
    }

    protected async Task SetStatusAsync()
    {
        var admin = await DbContext.FirstOrDefaultAsync<User>();

        if (admin == null)
        {
            MaxAllowedSlide = Slide.Welcome;
            await GoToSlideAsync(Slide.Welcome, true);
        }
        else
        {
            CreateAdminButtonText = Localizer["UpdateAdministratorAccount"];

            var identityUser = await UserManager.FindByIdAsync(admin.IdentityUserId!);
            if (identityUser != null && await UserManager.HasPasswordAsync(identityUser))
                MaxAllowedSlide = Slide.SetupSettings;
            else
                MaxAllowedSlide = Slide.SetAdminPassword;

            await GoToSlideAsync(MaxAllowedSlide, true);
        }

        StateHasChanged();
    }
    #endregion

    #region User Card
    protected virtual async Task<IBaseModel> GetAdminUser(OnEntryToBeShownByStartArgs args)
    {
        return (await args.EventServices.DbContext.FirstOrDefaultAsync<User>())!;
    }

    protected virtual Task UserCardOnAfterGetVisiblePropertiesArgs(OnAfterGetVisiblePropertiesArgs args)
    {
        var propertiesToRemove = new List<string>
        {
            nameof(User.PreferredCulture),
            nameof(User.PrefersDarkMode),
            nameof(User.IdentityRole),
            nameof(User.ProfileImage)
        };

        args.VisibleProperties.RemoveAll(property => propertiesToRemove.Contains(property.Name));
        return Task.CompletedTask;
    }

    #endregion


    #region Setup Card
    protected virtual async Task<IBaseModel> GetSetup(OnEntryToBeShownByStartArgs args)
    {
        return (await args.EventServices.DbContext.FirstOrDefaultAsync<Setup>())!;
    }

    protected virtual Task SetupCardOnAfterGetVisiblePropertiesArgs(OnAfterGetVisiblePropertiesArgs args)
    {
        var propertiesToRemove = new List<string>
        {
            nameof(User.PreferredCulture),
            nameof(User.PrefersDarkMode),
            nameof(User.IdentityRole)
        };

        args.VisibleProperties.RemoveAll(property => propertiesToRemove.Contains(property.Name));
        return Task.CompletedTask;
    }

    #endregion

    #region Actions



    protected async Task OnWelcomeStepNextClicked()
    {
        if (MaxAllowedSlide < Slide.Admin)
            MaxAllowedSlide = Slide.Admin;

        await GoRightAsync();
    }

    protected async Task OnCreateAdminClicked()
    {
        if (UserCard == null)
            return;

        var user = (User)UserCard.GetCurrentModel();
        user.IdentityRole = UserRole.Admin;
        user.IsCalledFromInitialSetupWizard = true;

        var success = await UserCard.SaveCardAsync();
        await UserCard.StateHasChangedAsync();
        if (!success)
            return;

        CreateAdminButtonText = Localizer["UpdateAdministratorAccount"];
        if (MaxAllowedSlide < Slide.SetAdminPassword)
            MaxAllowedSlide = Slide.SetAdminPassword;

        await GoRightAsync();
    }

    protected async Task OnSetAdminPasswordClicked()
    {
        if (UserCard == null)
            return;

        var identityUser = await UserManager.FindByIdAsync(((User)UserCard.GetCurrentModel()).IdentityUserId!);
        var result = await UserManager.RemovePasswordAsync(identityUser!);
        result = await UserManager.AddPasswordAsync(identityUser!, AdminPassword);
        if (result.Succeeded)
            ShowUserFeedback = false;
        else
        {
            UserFeedback = BaseMarkupStringValidator.GetWhiteListedMarkupString(String.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));
            ShowUserFeedback = true;
            return;
        }

        if (MaxAllowedSlide < Slide.SetupSettings)
            MaxAllowedSlide = Slide.SetupSettings;

        await GoRightAsync();
    }

    protected async Task OnSaveSetupClicked()
    {
        if (SetupCard == null)
            return;

        var setup = (Setup)SetupCard.GetCurrentModel();
        setup.SetSettingsFromInitialSetup();

        var success = await SetupCard.SaveCardAsync();
        await SetupCard.StateHasChangedAsync();
        if (!success)
            return;

        if (MaxAllowedSlide < Slide.CompleteSetup)
            MaxAllowedSlide = Slide.CompleteSetup;

        await GoRightAsync();
    }

    protected async Task OnCompleteSetupClicked()
    {
        var success = await UserCard!.SaveCardAsync();
        await UserCard.StateHasChangedAsync();
        if (!success)
        {
            await GoToSlideAsync(Slide.Admin);
            return;
        }

        var userModel = (User)UserCard.GetCurrentModel();
        var identityUser = await UserManager.FindByIdAsync(userModel.IdentityUserId!);
        if (!await UserManager.HasPasswordAsync(identityUser!) || ShowUserFeedback)
        {
            await GoToSlideAsync(Slide.SetAdminPassword);
            return;
        }

        success = await SetupCard!.SaveCardAsync();
        await SetupCard.StateHasChangedAsync();
        if (!success)
        {
            await GoToSlideAsync(Slide.SetupSettings);
            return;
        }

        await Setup.SetInitialSetupToCompletedAsync(DbContext);
        NavigationManager.NavigateTo("/", true);
    }

    protected async Task GoToSlideAsync(Slide slide, bool withoutCallingJs = false)
    {
        if (slide > MaxAllowedSlide)
            return;

        CurrentSlide = slide;
        ShowGoRightButton = CurrentSlide != MaxSlide && CurrentSlide != MaxAllowedSlide;
        ShowGoLeftButton = CurrentSlide != MinSlide;

        if (!withoutCallingJs)
            await JSRuntime.InvokeVoidAsync("SelectSlideByNumber", Carousel?.ElementId, (int)slide);
    }

    protected async Task GoRightAsync()
    {
        if (Carousel == null || CurrentSlide == MaxSlide || CurrentSlide == MaxAllowedSlide)
            return;

        CurrentSlide = CurrentSlide + 1;
        if (CurrentSlide == MaxSlide || CurrentSlide == MaxAllowedSlide)
            ShowGoRightButton = false;
        ShowGoLeftButton = true;

        await JSRuntime.InvokeVoidAsync("SelectNextSlide", Carousel.ElementId);
    }

    protected async Task GoLeftAsync()
    {
        if (Carousel == null || CurrentSlide == MinSlide)
            return;

        CurrentSlide = CurrentSlide - 1;
        if (CurrentSlide == MinSlide)
            ShowGoLeftButton = false;
        if (CurrentSlide != MaxAllowedSlide)
            ShowGoRightButton = true;

        await JSRuntime.InvokeVoidAsync("SelectPreviousSlide", Carousel.ElementId);
    }
    #endregion

}
