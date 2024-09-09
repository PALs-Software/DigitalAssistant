using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Models;
using BlazorBase.CRUD.Attributes;

namespace DigitalAssistant.Server.Modules.Commands.Models;

[HideBaseModelDatePropertiesInGUI]
public class CommandDisplayEntry : BaseModel
{
    [StickyColumn(Left = "0px")]
    [Visible(DisplayOrder = 100)]
    public string Name { get; set; } = null!;

    [Visible(DisplayOrder = 200)]
    public string Description { get; set; } = null!;

    [CustomClassAndStyle(Style = "width: 600px; overflow: auto; text-overflow: unset;")]
    [Visible(DisplayOrder = 300)]
    public string Template { get; set; } = null!;

    [CustomClassAndStyle(Style = "overflow: auto; text-overflow: unset;")]
    [Visible(DisplayOrder = 400)]
    public string Regex { get; set; } = null!;

    [CustomClassAndStyle(Style = "overflow: auto; text-overflow: unset;")]
    [Visible(DisplayOrder = 500)]
    public string? Parameters { get; set; }

    [CustomClassAndStyle(Style = "overflow: auto; text-overflow: unset;")]
    [Visible(DisplayOrder = 600)]
    public string? Options { get; set; }
}
