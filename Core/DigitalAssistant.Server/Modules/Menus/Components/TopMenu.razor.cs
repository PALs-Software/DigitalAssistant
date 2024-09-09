using BlazorBase.User.Models;
using DigitalAssistant.Server.Modules.Commands.Components;
using DigitalAssistant.Server.Modules.Menus.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class TopMenu
{
    #region Injects
    [Inject] protected IStringLocalizer<UserMenu> Localizer { get; set; } = null!;
    [Inject] protected IBlazorBaseUserOptions Options { get; set; } = null!;
    #endregion

    #region Members
    private BackPanel? BackPanel;
    private ChatModal? ChatModal;

    private TopSideMenuType SideMenuType = TopSideMenuType.Hide;
    #endregion

    private void ToogleSideMenu(TopSideMenuType type)
    {
        if (SideMenuType == type)
            SideMenuType = TopSideMenuType.Hide;
        else
            SideMenuType = type;

        BackPanel?.SetVisibility(SideMenuType != TopSideMenuType.Hide);
        InvokeAsync(StateHasChanged);
    }

    private void OnBackPanelClicked()
    {
        if (SideMenuType != TopSideMenuType.Hide)
            ToogleSideMenu(TopSideMenuType.Hide);
    }

}
