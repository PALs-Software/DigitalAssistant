using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.Abstractions.CRUD.Enums;
using BlazorBase.CRUD.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalAssistant.Server.Modules.Telemetry.Tasks;

[HideBaseModelDatePropertiesInGUI]
[Index(nameof(CreatedOn))]
public class ApiTelemetryEntry : BaseModel
{
    #region Properties

    #region Key

    [Key]
    [Required]
    [Visible(DisplayOrder = 100)]
    public string Name { get; set; } = null!;

    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EntryNo { get; set; }

    #endregion

    [Required]
    [Visible(DisplayOrder = 200)]
    public int Count { get; set; }

    [Required]
    [Visible(DisplayOrder = 300)]
    public int ErrorCount { get; set; }

    [Visible(DisplayOrder = 400)]
    public string? LastErrorMessage { get; set; }

    [Required]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayOrder = 500)]
    public DateTime LastRequest { get; set; }

    [Required]
    [PresentationDataType(PresentationDataType.DateTime)]
    [Visible(DisplayOrder = 600)]
    public DateTime LastErrorRequest { get; set; }

    #endregion

}