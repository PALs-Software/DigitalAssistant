using BlazorBase.CRUD.Models;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Connectors.Models;

public class ConnectorSettings : BaseModel
{
    #region Properties
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Type { get; set; } = null!;

    public string? SettingsAsJson { get; set; }

    #endregion
}
