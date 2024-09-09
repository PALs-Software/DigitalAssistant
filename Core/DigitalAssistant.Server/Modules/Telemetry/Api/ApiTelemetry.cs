using BlazorBase.Abstractions.CRUD.Extensions;
using BlazorBase.CRUD.Extensions;
using DigitalAssistant.Server.Modules.Telemetry.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace DigitalAssistant.Server.Modules.Telemetry.Tasks;

[Route("/")]
[Authorize(Roles = "Admin")]
public class ApiTelemetry : CountTelemetryList<ApiTelemetryEntry>
{
    #region Members

    protected override string Title => "Api Telemetry";
    protected override bool ShowDateFilters => true;
    protected override bool ShowCustomerEnvironmentSelectionFilters => false;

    #endregion

    protected override Task<List<ApiTelemetryEntry>> OnRefreshTelemetryEntriesAsync()
    {
        return DbContext.SetAsync((IQueryable<ApiTelemetryEntry> query) =>
        {
            if (StartDate != null)
                query = query.Where(taskEntry => taskEntry.CreatedOn >= StartDate);
            if (EndDate != null)
                query = query.Where(taskEntry => taskEntry.CreatedOn <= EndDate);

            return query
               .GroupBy(entry => entry.Name)
               .Select(entry => new ApiTelemetryEntry
               {
                   Name = entry.Key,
                   Count = entry.Sum(e => e.Count),
                   ErrorCount = entry.Sum(e => e.ErrorCount),
                   LastRequest = entry.OrderBy(e => e.Name).ThenBy(e => e.EntryNo).Last().LastRequest,
                   LastErrorRequest = entry.OrderBy(e => e.Name).ThenBy(e => e.EntryNo).Where(e => e.LastErrorRequest != DateTime.MinValue).Select(e => e.LastErrorRequest).LastOrDefault(),
                   LastErrorMessage = entry.OrderBy(e => e.Name).ThenBy(e => e.EntryNo).Where(e => e.LastErrorMessage != null).Select(e => e.LastErrorMessage).LastOrDefault()
               })
               .ToList();
        });
    }
}
