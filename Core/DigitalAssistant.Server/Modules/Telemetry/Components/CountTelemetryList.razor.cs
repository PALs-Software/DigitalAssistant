using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.MessageHandling.Interfaces;
using BlazorBase.Modules;
using DigitalAssistant.Server.Modules.Telemetry.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace DigitalAssistant.Server.Modules.Telemetry.Components;

public abstract partial class CountTelemetryList<T> where T : class, IBaseModel, new()
{
    #region Injects

    [Inject] protected IBaseDbContext DbContext { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected IStringLocalizer<CountTelemetryList<T>> Localizer { get; set; } = null!;
    [Inject] protected IStringLocalizer<TelemetryDateFilterType> TelemetryDateFilterTypeLocalizer { get; set; } = null!;
    [Inject] protected IMessageHandler MessageHandler { get; set; } = null!;

    #endregion

    #region Members
    protected abstract string Title { get; }
    protected abstract bool ShowDateFilters { get; }
    protected abstract bool ShowCustomerEnvironmentSelectionFilters { get; }
    protected virtual bool ShowOnlyCustomerGroups => false;

    protected DateTime? StartDate;
    protected DateTime? EndDate;

    protected BaseObservableCollection<T> TelemetryEntries = [];
    protected List<KeyValuePair<TelemetryDateFilterType?, string>> DateFilterTypes = [];

    protected TelemetryDateFilterType? SelectedDateFilterType;
    #endregion

    protected override Task OnInitializedAsync()
    {
        DateFilterTypes = GetTelemetryDateFilterTypes().ToList();
        return RefreshTelemetryEntriesAsync();
    }

    #region Filter

    protected Task OnTimeFilterChangedAsync(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        SelectedDateFilterType = null;

        return RefreshTelemetryEntriesAsync();
    }

    protected Task OnSelectedDateFilterTypeChangedAsync(TelemetryDateFilterType? value)
    {
        SelectedDateFilterType = value;

        switch (SelectedDateFilterType)
        {
            case TelemetryDateFilterType.LastDay:
                StartDate = DateTime.Now.Date.AddDays(-1);
                EndDate = null;
                break;
            case TelemetryDateFilterType.LastWeek:
                StartDate = DateTime.Now.Date.AddDays(-7);
                EndDate = null;
                break;
            case TelemetryDateFilterType.LastMonth:
                StartDate = DateTime.Now.Date.AddMonths(-1);
                EndDate = null;
                break;
            case TelemetryDateFilterType.LastYear:
                StartDate = DateTime.Now.Date.AddYears(-1);
                EndDate = null;
                break;
            default:
                StartDate = null;
                EndDate = null;
                break;
        }

        return RefreshTelemetryEntriesAsync();
    }

    #endregion

    #region Refresh Data

    protected abstract Task<List<T>> OnRefreshTelemetryEntriesAsync();

    protected async Task RefreshTelemetryEntriesAsync()
    {
        var loadingMessageId = MessageHandler.ShowLoadingMessage(Localizer["Calculate telemetry..."]);

        TelemetryEntries.ReplaceItems(await OnRefreshTelemetryEntriesAsync());

        MessageHandler.CloseLoadingMessage(loadingMessageId);
    }

    #endregion

    #region MISC

    protected IEnumerable<KeyValuePair<TelemetryDateFilterType?, string>> GetTelemetryDateFilterTypes()
    {
        yield return new KeyValuePair<TelemetryDateFilterType?, string>(null, String.Empty);

        foreach (var value in Enum.GetValues<TelemetryDateFilterType>())
            yield return new KeyValuePair<TelemetryDateFilterType?, string>(value, TelemetryDateFilterTypeLocalizer[Enum.GetName(value) ?? String.Empty]);
    }

    #endregion
}