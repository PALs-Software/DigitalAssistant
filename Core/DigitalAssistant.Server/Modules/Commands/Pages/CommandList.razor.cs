using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.Modules;
using DigitalAssistant.Server.Modules.Commands.Models;
using DigitalAssistant.Server.Modules.Commands.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Pages;

[Route("/Commands")]
[Authorize(Roles = "Admin")]
public partial class CommandList
{
    #region Injects
    [Inject] protected CommandHandler CommandHandler { get; set; } = null!;
    [Inject] protected IStringLocalizer<CommandList> Localizer { get; set; } = null!;
    [Inject] protected IMessageHandler MessageHandler { get; set; } = null!;
    #endregion

    #region Members
    protected BaseObservableCollection<CommandDisplayEntry> CommandEntries = [];
    #endregion

    protected override Task OnInitializedAsync()
    {
        return RefreshEntriesAsync();
    }

    #region Refresh Data

    protected async Task RefreshEntriesAsync()
    {
        var loadingMessageId = MessageHandler.ShowLoadingMessage(Localizer["Loading commands..."]);

        var commands = await CommandHandler.GetLocalizedCommandTemplatesAsync(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        var commandDisplayEntries = new List<CommandDisplayEntry>();

        foreach (var command in commands)
        {
            foreach (var commandTemplate in command)
            {
                commandDisplayEntries.Add(new CommandDisplayEntry
                {
                    Name = commandTemplate.Command.GetName(),
                    Description = commandTemplate.Command.GetDescription(),
                    Template = commandTemplate.Template,
                    Regex = commandTemplate.Regex.ToString(),
                    Parameters = String.Join(", ", commandTemplate.Parameters.Select(entry => $"{entry.Value.Name}:{entry.Value.Type}")),
                    Options = commandTemplate.Command.GetOptionsJson()
                });
            }
        }

        CommandEntries.ReplaceItems(commandDisplayEntries);
        MessageHandler.CloseLoadingMessage(loadingMessageId);
    }

    #endregion
}