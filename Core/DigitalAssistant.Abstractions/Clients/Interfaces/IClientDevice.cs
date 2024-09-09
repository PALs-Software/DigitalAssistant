using DigitalAssistant.Abstractions.Clients.Enums;

namespace DigitalAssistant.Abstractions.Clients.Interfaces;

public interface IClient
{
    Guid Id { get; set; }
    string Name { get; set; }
    ClientType Type { get; set; }
}