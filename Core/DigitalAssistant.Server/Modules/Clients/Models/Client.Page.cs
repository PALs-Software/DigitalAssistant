using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Structures;
using Blazorise.Icons.FontAwesome;
using DigitalAssistant.Server.Modules.Clients.Components;

namespace DigitalAssistant.Server.Modules.Clients.Models;

public partial class Client
{
    #region Page Configuration
    public override bool UserCanAddEntries => false;
    #endregion

    public override Task<List<PageActionGroup>?> GeneratePageActionGroupsAsync(EventServices eventServices)
    {
        return Task.FromResult<List<PageActionGroup>?>(
        [
            new PageActionGroup()
            {
                Caption = PageActionGroup.DefaultGroups.Process,
                VisibleInGUITypes = [GUIType.List],
                PageActions =
                [
                    new PageAction()
                    {
                        Caption = "Add new Client",
                        ToolTip = "Adds a new client to the server",
                        Image = FontAwesomeIcons.Plus,
                        VisibleInGUITypes = [GUIType.List],
                        RenderComponentByActionArgs = new RenderComponentByActionArgs()
                        {
                            ComponentType = typeof(AddClientModal),
                            OnComponentRemoved = (source, eventServices, model, result) =>
                            {
                                _ = InvokeAsync(StateHasChanged);
                                return Task.CompletedTask;
                            }
                        }
                    }
                ]
            }
        ]);
    }
}
