using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Services.OptionsService;

namespace SqlQueryStress.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
    private readonly IOptionsService _optionsService;

    [ObservableProperty] public partial bool EnableConnectionPooling { get; set; }
    [ObservableProperty] public partial bool CollectIOStatistics { get; set; }
    [ObservableProperty] public partial bool CollectTimeStatistics { get; set; }
    [ObservableProperty] public partial bool ForceClientRetrievalOfData { get; set; }
    [ObservableProperty] public partial bool KillQueriesOnCancel { get; set; }
    [ObservableProperty] public partial int ConnectionTimeout { get; set; }
    [ObservableProperty] public partial int CommandTimeout { get; set; }

    public OptionsViewModel(IOptionsService optionsService)
    {
        _optionsService = optionsService;

        EnableConnectionPooling = _optionsService.EnableConnectionPooling;
        CollectIOStatistics = _optionsService.CollectIOStatistics;
        CollectTimeStatistics = _optionsService.CollectTimeStatistics;
        ForceClientRetrievalOfData = _optionsService.ForceClientRetrievalOfData;
        KillQueriesOnCancel = _optionsService.KillQueriesOnCancel;
        ConnectionTimeout = _optionsService.ConnectionTimeout;
        CommandTimeout = _optionsService.CommandTimeout;
    }

    /// <summary>
    /// Apply any updates to the app options
    /// </summary>
    public void ApplyChanges()
    {
        _optionsService.EnableConnectionPooling = EnableConnectionPooling;
        _optionsService.CollectIOStatistics = CollectIOStatistics;
        _optionsService.CollectTimeStatistics = CollectTimeStatistics;
        _optionsService.ForceClientRetrievalOfData = ForceClientRetrievalOfData;
        _optionsService.KillQueriesOnCancel = KillQueriesOnCancel;
        _optionsService.ConnectionTimeout = ConnectionTimeout;
        _optionsService.CommandTimeout = CommandTimeout;
    }
}