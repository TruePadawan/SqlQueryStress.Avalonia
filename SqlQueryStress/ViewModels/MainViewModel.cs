using System.ComponentModel.DataAnnotations;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlQueryStress.Services.QuerySettingsService;

namespace SqlQueryStress.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IQuerySettingsService _querySettingsService;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Number of iterations must be at least 1")]
    public partial int NumIterations { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Number of threads must be at least 1")]
    public partial int NumThreads { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, int.MaxValue, ErrorMessage = "Delay between queries cannot be negative")]
    public partial int DelayBetweenQueries { get; set; }

    // Work-around for AvaloniaEdit's Text property not being bindable
    [ObservableProperty] public partial TextDocument MainQueryDocument { get; set; }


    public MainViewModel(IQuerySettingsService querySettingsService)
    {
        _querySettingsService = querySettingsService;

        NumIterations = _querySettingsService.NumIterations;
        NumThreads = _querySettingsService.NumThreads;
        DelayBetweenQueries = _querySettingsService.DelayBetweenQueries;

        MainQueryDocument = new TextDocument(_querySettingsService.MainQuery);

        // Update the query settings service when the query text changes
        MainQueryDocument.TextChanged += (_, _) =>
            _querySettingsService.MainQuery = MainQueryDocument.Text;
    }

    partial void OnNumIterationsChanged(int value)
    {
        if (!HasErrors) _querySettingsService.NumIterations = value;
    }

    partial void OnNumThreadsChanged(int value)
    {
        if (!HasErrors) _querySettingsService.NumThreads = value;
    }

    partial void OnDelayBetweenQueriesChanged(int value)
    {
        if (!HasErrors) _querySettingsService.DelayBetweenQueries = value;
    }
}