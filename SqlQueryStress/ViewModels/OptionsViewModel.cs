using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Services.OptionsService;

namespace SqlQueryStress.ViewModels;

public partial class OptionsViewModel : ViewModelBase
{
    private readonly IOptionsService _optionsService;

    [ObservableProperty] private bool _enableConnectionPooling;
    [ObservableProperty] private bool _collectIOStatistics;
    [ObservableProperty] private bool _collectTimeStatistics;
    [ObservableProperty] private bool _forceClientRetrievalOfData;
    [ObservableProperty] private bool _killQueriesOnCancel;
    [ObservableProperty] private int _connectionTimeout;
    [ObservableProperty] private int _commandTimeout;

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