using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.HueConnector.Components;

public partial class LoadingIndicator
{
    [Parameter] public RenderFragment? ChildContent { get; set; } = null;
    [Parameter] public bool Visible { get; set; }
}
