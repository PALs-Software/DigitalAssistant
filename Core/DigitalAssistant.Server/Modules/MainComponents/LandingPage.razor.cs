using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.MainComponents;

public partial class LandingPage
{
    #region Injects
    [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
    #endregion

    protected override void OnAfterRender(bool firstRender)
    {
        NavigationManager.NavigateTo($"/Dashboard");
    }
}
