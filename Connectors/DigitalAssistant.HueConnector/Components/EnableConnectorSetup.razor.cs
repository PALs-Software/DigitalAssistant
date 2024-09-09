using DigitalAssistant.Abstractions.Connectors;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.HueConnector.Components;

public partial class EnableConnectorSetup
{
    #region Parameter
    [Parameter] public IConnector Connector { get; set; } = null!;
    [Parameter] public EventCallback<(bool Success, IConnectorSettings? Settings)> OnConnectorSetupFinished { get; set; }
    #endregion

    #region Injects
    [Inject] public IStringLocalizer<EnableConnectorSetup> Localizer { get; set; } = null!;
    #endregion

    #region Member
    protected Services.HueConnector? HueConnector = null!;
    protected bool CheckConnector = true;
    protected bool ConnectorIsAvailable = false;
    protected string? ErrorMessage;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        HueConnector = Connector as Services.HueConnector;
        try
        {
            ConnectorIsAvailable = HueConnector != null && (await HueConnector.IsAvailableAsync()).IsAvailable;
        }
        catch (Exception)
        {
            ConnectorIsAvailable = false;
        }

        CheckConnector = false;
    }

    protected async Task EnableConnectorAsync()
    {
        if (HueConnector == null)
            return;

        if (HueConnector.Enabled)
        {
            CloseSetup(true, HueConnector.Settings);
            return;
        }

        try
        {
            var result = await HueConnector.RegisterAsync();
            if (result.Success)
                CloseSetup(true, result.Settings);
            else
                ErrorMessage = result.ErrorMessage;
        }
        catch (Exception e)
        {
            ErrorMessage = PrepareExceptionErrorMessage(e);
        }
    }

    protected void CloseSetup(bool success, IConnectorSettings? settings)
    {
        OnConnectorSetupFinished.InvokeAsync((success, settings));
    }

    #region MISC
    protected string PrepareExceptionErrorMessage(Exception e)
    {
        if (e.InnerException == null)
            return e.Message;

        return e.Message + Environment.NewLine + Environment.NewLine + Localizer["Inner Exception:"] + PrepareExceptionErrorMessage(e.InnerException);
    }
    #endregion
}
