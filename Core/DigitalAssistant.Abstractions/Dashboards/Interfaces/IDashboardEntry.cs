namespace DigitalAssistant.Abstractions.Dashboards.Interfaces;

public interface IDashboardEntry
{
    string Name { get; set; }
    bool ShowInDashboard { get; set; }
    int DashboardOrder { get; set; }
}
