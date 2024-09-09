using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class UserProfilImage : ComponentBase
{
    #region Injects

    #endregion

    #region Members
    public string? ProfileImageBase64String = null;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        ProfileImageBase64String = null;
    }
}