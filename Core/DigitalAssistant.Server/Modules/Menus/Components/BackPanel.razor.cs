using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Menus.Components;

public partial class BackPanel
{
    #region Parameter
    [Parameter] public EventCallback OnBackPanelClicked { get; set; }
    #endregion

    #region Members
    protected bool IsVisible = false;
    #endregion

    public void SetVisibility(bool visible)
    {
        IsVisible = visible;
        InvokeAsync(StateHasChanged);
    }

    protected Task BackPanelClickedAsync()
    {
        return OnBackPanelClicked.InvokeAsync();
    }
}
