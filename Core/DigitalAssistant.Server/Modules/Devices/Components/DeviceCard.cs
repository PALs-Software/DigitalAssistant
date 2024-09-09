using BlazorBase.Abstractions.CRUD.Arguments;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.CRUD.Components.Card;
using DigitalAssistant.Server.Modules.Devices.Models;

namespace DigitalAssistant.Server.Modules.Devices.Components;

public class DeviceCard : BaseCard<Device>
{
    public override async Task ShowAsync(bool addingMode, bool viewMode, object?[]? primaryKeys = null, IBaseModel? template = null)
    {
        await base.ShowAsync(addingMode, viewMode, primaryKeys, template);

        await SetUpDisplayListsAsync(Model?.GetType() ?? typeof(Device), GUIType.Card, null);

        if (Model != null)
            await Model.OnShowEntry(new OnShowEntryArgs(GUIType.Card, Model, addingMode, viewMode, VisibleProperties, DisplayGroups, EventServices));
    }
}