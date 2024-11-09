using BlazorBase.Abstractions.General.Extensions;
using DigitalAssistant.Abstractions.Connectors;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Runtime.Versioning;
using System.Security;

namespace DigitalAssistant.HomeAssistantConnector.Components;

[UnsupportedOSPlatform("browser")]
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
    protected Services.HomeAssistantConnector? HaConnector = null!;
    protected string? ErrorMessage;
    protected bool ShowLoadingIndicator = false;

    protected string HaUrl = String.Empty;
    protected string HaAccessToken { get => HaAccessTokenSecure?.ToInsecureString() ?? String.Empty; set => HaAccessTokenSecure = String.IsNullOrWhiteSpace(value) ? null : value.ToSecureString(); }
    protected SecureString? HaAccessTokenSecure;
    #endregion

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        HaConnector = (Services.HomeAssistantConnector) Connector;
    }

    protected async Task EnableConnectorAsync()
    {
        if (HaConnector == null)
            return;

        if (HaConnector.Enabled)
        {
            CloseSetup(true, HaConnector.Settings);
            return;
        }

        if (String.IsNullOrWhiteSpace(HaUrl))
        {
            ErrorMessage = Localizer["UrlIsRequiredError"];
            return;
        }

        if (HaAccessTokenSecure == null || String.IsNullOrWhiteSpace(HaAccessToken))
        {
            ErrorMessage = Localizer["AccessTokenIsRequiredError"];
            return;
        }

        try
        {
            ShowLoadingIndicator = true;
            await InvokeAsync(StateHasChanged);
            var result = await HaConnector.RegisterAsync(HaUrl, HaAccessTokenSecure);
            if (result.Success)
                CloseSetup(true, result.Settings);
            else
                ErrorMessage = Localizer[result.ErrorMessage ?? String.Empty];
        }
        catch (Exception e)
        {
            ErrorMessage = PrepareExceptionErrorMessage(e);
        }

        ShowLoadingIndicator = false;
        await InvokeAsync(StateHasChanged);
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

        return e.Message + Environment.NewLine + Environment.NewLine + Localizer["InnerException"] + PrepareExceptionErrorMessage(e.InnerException);
    }
    #endregion
}
