using BlazorBase.Abstractions.CRUD.Attributes;
using BlazorBase.CRUD.Models;
using DigitalAssistant.Abstractions.Clients.Enums;
using DigitalAssistant.Abstractions.Clients.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace DigitalAssistant.Server.Modules.Clients.Models;

public class ClientBase : BaseModel, IClient
{
    #region Constructors
   
    public ClientBase() { }
    public ClientBase(Client client)
    {
        Id = client.Id;
        Name = client.Name;
        Type = client.Type;
    }
    public ClientBase(Guid id, string name, ClientType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public static IClient Browser { get; } = new ClientBase(Guid.Empty, "Browser", ClientType.Browser);

    #endregion

    #region Properties

    [Key]
    public Guid Id { get; set; }

    [Visible(DisplayOrder = 100)]
    [Required]
    [DisplayKey]
    [StringLength(250)]
    public string Name { get; set; } = null!;

    [Visible(DisplayOrder = 200)]
    [Required]
    public ClientType Type { get; set; }

    #endregion
}
