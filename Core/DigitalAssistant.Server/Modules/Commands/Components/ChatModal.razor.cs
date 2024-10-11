using Blazorise;
using DigitalAssistant.Server.Modules.Ai.Asr.Components;
using DigitalAssistant.Server.Modules.Clients.Models;
using DigitalAssistant.Server.Modules.Commands.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Components;
public partial class ChatModal
{
    #region Injects
    [Inject] protected IStringLocalizer<ChatModal> Localizer { get; set; } = null!;
    [Inject] protected CommandProcessor CommandProcessor { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    #endregion

    #region Members
    protected Modal? Modal;
    protected bool ModalVisibilityChanged = false;
    protected ElementReference? UserInput;
    protected AsrAudioRecorder? AsrAudioRecorder;

    protected List<(string Message, bool FromUser)> Messages = [];
    protected string? CurrentMessage;
    protected bool MessagesChanged = false;
    protected bool CommandIsExecuting = false;
    protected bool DebugModusEnabled = false;
    #endregion

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (MessagesChanged)
        {
            MessagesChanged = false;
            await ScrollChatBoxToTheBottomAsync();
        }

        if (ModalVisibilityChanged)
        {
            ModalVisibilityChanged = false;
            if (UserInput != null)
                await UserInput.Value.FocusAsync();
        }
    }

    public void Show()
    {
        Modal?.Show();
        ModalVisibilityChanged = true;
        StateHasChanged();
    }

    protected Task OnInputKeyUpAsync(KeyboardEventArgs e)
    {
        if (e.Code != "Enter" && e.Code != "NumpadEnter")
            return Task.CompletedTask;

        return HandleCurrentMessageAsync();
    }

    protected Task HandleCurrentMessageAsync()
    {
        var message = CurrentMessage;
        CurrentMessage = null;
        return HandleMessageAsync(message);
    }

    protected Task OnNewAudioRecorderDataAsync(string message)
    {
        return HandleMessageAsync(message);
    }

    protected async Task HandleMessageAsync(string? message)
    {
        message = message?.Trim();
        if (String.IsNullOrEmpty(message))
            return;

        AddMessage(message, true);
        CommandIsExecuting = true;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(100);

        string? response;
        if (DebugModusEnabled)
        {
            response = "Debug result:" + Environment.NewLine;
            response += $" - Recording Time: {AsrAudioRecorder?.DebugInfos?.RecordingTime}ms" + Environment.NewLine;
            response += $" - Asr Conversion Time: {AsrAudioRecorder?.DebugInfos?.AsrConversionTime}ms" + Environment.NewLine;
            response += await CommandProcessor.ProcessUserCommandDebugAsync(message, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, ClientBase.Browser, ServiceProvider);
        }
        else
            response = await CommandProcessor.ProcessUserCommandAsync(message, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, ClientBase.Browser, ServiceProvider);

        CommandIsExecuting = false;

        if (!String.IsNullOrEmpty(response))
            AddMessage(response, false);

        await InvokeAsync(StateHasChanged);
    }

    protected void ToggleDebugModus()
    {
        DebugModusEnabled = !DebugModusEnabled;
    }

    #region MISC

    protected ValueTask ScrollChatBoxToTheBottomAsync()
    {
        return JSRuntime.InvokeVoidAsync("DA.ScrollElementToBottom", "chat-messages");
    }

    protected void AddMessage(string message, bool fromUser)
    {
        Messages.Add((message, fromUser));
        MessagesChanged = true;
    }

    #endregion
}
