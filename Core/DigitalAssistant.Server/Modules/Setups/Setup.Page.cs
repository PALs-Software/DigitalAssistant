using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Structures;
using BlazorBase.Backup.Services;
using Blazorise.Icons.FontAwesome;

namespace DigitalAssistant.Server.Modules.Setups.Models;

public partial class Setup
{
    #region Page Configuration
    public override bool ShowOnlySingleEntry => true;
    #endregion

    public override Task<List<PageActionGroup>?> GeneratePageActionGroupsAsync(EventServices eventServices)
    {
        return Task.FromResult<List<PageActionGroup>?>(
        [
            new PageActionGroup()
            {
                Caption = PageActionGroup.DefaultGroups.Process,
                VisibleInGUITypes = [GUIType.Card],
                PageActions =
                [
                    new PageAction()
                    {
                        Caption = "CreateAndDownloadBackupAction",
                        ToolTip = "CreateAndDownloadBackupActionTooltip",
                        Image = FontAwesomeIcons.Database,
                        VisibleInGUITypes = [GUIType.Card],
                        Action = (source, eventServices, model) =>
                        {
                            var backupWebsiteService = eventServices.ServiceProvider.GetRequiredService<BackupWebsiteService>();
                            return backupWebsiteService.CreateAndDownloadWebsiteBackupAsync();
                        }
                    }
                ]
            }
        ]);
    }
}
