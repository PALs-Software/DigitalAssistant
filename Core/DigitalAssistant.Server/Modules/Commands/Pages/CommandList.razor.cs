using BlazorBase.MessageHandling.Interfaces;
using DigitalAssistant.Server.Modules.Commands.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DigitalAssistant.Server.Modules.Commands.Pages;

[Route("/Commands")]
[Authorize(Roles = "Admin, User")]
public partial class CommandList
{
    #region Injects
    [Inject] protected CommandHandler CommandHandler { get; set; } = null!;
    [Inject] protected IStringLocalizer<CommandList> Localizer { get; set; } = null!;
    [Inject] protected IMessageHandler MessageHandler { get; set; } = null!;
    #endregion

    #region Members
    protected record CommandDisplayGroup(string Name, string Description, int Priority, List<CommandDisplayEntry> Entries);
    protected record CommandDisplayEntry(string Template, string Regex, string? Parameters, string? Options);

    protected List<CommandDisplayGroup> CommandGroups = [];
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
        var commandGroups = new List<CommandDisplayGroup>();
        foreach (var command in commands)
        {
            if (command.Count == 0)
                continue;

            var commandDisplayEntries = new List<CommandDisplayEntry>();
            foreach (var commandTemplate in command)
            {
                commandDisplayEntries.Add(new CommandDisplayEntry(
                    commandTemplate.Template,
                    commandTemplate.Regex.ToString(),
                    String.Join(", ", commandTemplate.Parameters.Select(entry => $"{entry.Value.Name}:{entry.Value.Type}")),
                    commandTemplate.Command.GetOptionsJson()
                ));
            }

            commandGroups.Add(new CommandDisplayGroup(command[0].Command.GetName(), command[0].Command.GetDescription(), command[0].Command.Priority, commandDisplayEntries));
        }

        CommandGroups = commandGroups.OrderBy(entry => entry.Priority).ToList();
        MessageHandler.CloseLoadingMessage(loadingMessageId);
    }

    #endregion
}