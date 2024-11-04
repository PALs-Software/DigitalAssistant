using DigitalAssistant.Abstractions.Clients.Interfaces;
using DigitalAssistant.Abstractions.Devices.Interfaces;

namespace DigitalAssistant.Abstractions.Groups.Interfaces;

public interface IGroup
{
    Guid Id { get; set; }
    string Name { get; set; }
    bool ShowInDashboard { get; set; }
    int DashboardOrder { get; set; }
    string? Description { get; set; }
    List<string> AlternativeNames { get; set; }
    List<IDevice> Devices { get; }
    List<IClient> Clients { get; }
}
