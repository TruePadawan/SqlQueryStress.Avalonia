using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.LoadEngine;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.Services.QuerySettingsService;
using SqlQueryStress.Services.SettingsFileService;

namespace SqlQueryStress.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IQuerySettingsService _querySettingsService;
    private readonly ISettingsFileService _settingsFileService;
    private readonly IDbConnectionService _dbConnectionService;
    private readonly IOptionsService _optionsService;

    /// <summary>
    /// Set by the View after creation to enable file picker dialogs from commands.
    /// </summary>
    public IStorageProvider? StorageProvider { get; set; }

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
    [ObservableProperty] public partial bool IsRunning { get; set; }
    [ObservableProperty] public partial string ElapsedTimeText { get; set; } = "0";
    [ObservableProperty] public partial string IterationsCompletedText { get; set; } = "0";
    [ObservableProperty] public partial string AverageClientSecondsText { get; set; } = "0";
    [ObservableProperty] public partial string TotalExceptionsText { get; set; } = "0";
    [ObservableProperty] public partial string AverageCpuSecondsText { get; set; } = "0";
    [ObservableProperty] public partial string AverageLogicalReadsText { get; set; } = "0";
    [ObservableProperty] public partial string AverageActualSecondsText { get; set; } = "0";
    [ObservableProperty] public partial string ActiveThreadsText { get; set; } = "0";
    [ObservableProperty] public partial double ProgressValue { get; set; }
    [ObservableProperty] public partial double ProgressMaximum { get; set; } = 1;

    private CancellationTokenSource? _runCancellation;
    private int _runVersion;

    public MainViewModel(
        IQuerySettingsService querySettingsService,
        ISettingsFileService settingsFileService,
        IDbConnectionService dbConnectionService,
        IOptionsService optionsService
    )
    {
        _querySettingsService = querySettingsService;
        _settingsFileService = settingsFileService;
        _dbConnectionService = dbConnectionService;
        _optionsService = optionsService;

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

    /// <summary>
    /// Refreshes all properties from the query settings service (e.g. after loading settings).
    /// </summary>
    private void RefreshFromService()
    {
        NumIterations = _querySettingsService.NumIterations;
        NumThreads = _querySettingsService.NumThreads;
        DelayBetweenQueries = _querySettingsService.DelayBetweenQueries;

        if (MainQueryDocument.Text != _querySettingsService.MainQuery)
        {
            MainQueryDocument.Text = _querySettingsService.MainQuery;
        }
    }

    [RelayCommand]
    private async Task GoAsync()
    {
        if (IsRunning || HasInvalidQuerySettings() || string.IsNullOrWhiteSpace(MainQueryDocument.Text))
            return;

        IsRunning = true;
        _runCancellation = new CancellationTokenSource();

        var cancellationToken = _runCancellation.Token;
        var runVersion = Interlocked.Increment(ref _runVersion);
        var stopwatch = new Stopwatch();
        using var engine = new LoadEngine();
        var iterations = NumIterations;
        var threadCount = NumThreads;
        var startupError = false;

        try
        {
            var connectionInfo = _dbConnectionService.MainDbConnectionInfo with
            {
                EnablePooling = _optionsService.EnableConnectionPooling
            };

            var connectionString = ConnectionStringBuilder.Build(
                connectionInfo,
                _optionsService.ConnectionTimeout);

            var query = MainQueryDocument.Text;
            var queryDelay = DelayBetweenQueries;
            var collectIoStats = _optionsService.CollectIOStatistics;
            var collectTimeStats = _optionsService.CollectTimeStatistics;

            ResetStatistics(threadCount * iterations, threadCount);

            for (var i = 0; i < threadCount; i++)
            {
                var connection = new SqlConnection(connectionString);
                var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = _optionsService.CommandTimeout
                };

                var worker = new QueryWorker(
                    connection,
                    command,
                    iterations,
                    output => PublishOutput(engine, output, stopwatch, threadCount, iterations, runVersion),
                    queryDelay: queryDelay,
                    cancellationToken: cancellationToken,
                    collectIoStats: collectIoStats,
                    collectTimeStats: collectTimeStats,
                    forceDataRetrieval: _optionsService.ForceClientRetrievalOfData,
                    killQueriesOnCancel: _optionsService.KillQueriesOnCancel);

                engine.AddWorker(worker);
            }

            stopwatch.Start();
            engine.Start();

            // The engine's workers still run on their own dedicated Threads.
            await Task.Run(engine.WaitForCompletion);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Query execution failed: {exception}");
            startupError = true;
        }
        finally
        {
            stopwatch.Stop();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (runVersion == Volatile.Read(ref _runVersion))
                {
                    UpdateStatistics(engine.Outputs, stopwatch.Elapsed, threadCount, iterations, activeThreadCount: 0);

                    if (startupError)
                        TotalExceptionsText = "1";
                }

                IsRunning = false;
            });

            _runCancellation.Dispose();
            _runCancellation = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _runCancellation?.Cancel();
    }

    private void PublishOutput(
        LoadEngine engine,
        QueryOutput output,
        Stopwatch stopwatch,
        int threadCount,
        int iterations,
        int runVersion)
    {
        engine.PublishOutput(output);

        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (runVersion == Volatile.Read(ref _runVersion))
                UpdateStatistics(engine.Outputs, stopwatch.Elapsed, threadCount, iterations, engine.ActiveThreadCount);
        });
    }

    private void ResetStatistics(int totalIterations, int threadCount)
    {
        ElapsedTimeText = "0";
        IterationsCompletedText = $"0 / {totalIterations}";
        AverageClientSecondsText = "0";
        TotalExceptionsText = "0";
        AverageCpuSecondsText = "0";
        AverageLogicalReadsText = "0";
        AverageActualSecondsText = "0";
        ActiveThreadsText = totalIterations > 0 ? threadCount.ToString() : "0";
        ProgressValue = 0;
        ProgressMaximum = Math.Max(1, totalIterations);
    }

    private void UpdateStatistics(
        System.Collections.Generic.IReadOnlyList<QueryOutput> outputs,
        TimeSpan elapsed,
        int threadCount,
        int iterations,
        int? activeThreadCount = null)
    {
        var completed = outputs.Count;
        var divisor = Math.Max(1, completed);
        var successful = outputs.Where(output => output.Error is null).ToArray();
        var outputsWithIoStatistics = successful.Where(output => output.HasIoStatistics).ToArray();
        var outputsWithTimeStatistics = successful.Where(output => output.HasTimeStatistics).ToArray();
        var ioStatisticsDivisor = Math.Max(1, outputsWithIoStatistics.Length);
        var timeStatisticsDivisor = Math.Max(1, outputsWithTimeStatistics.Length);

        ElapsedTimeText = elapsed.TotalSeconds.ToString("F2");
        IterationsCompletedText = $"{completed} / {threadCount * iterations}";
        TotalExceptionsText = outputs.Count(output => output.Error is not null).ToString();

        AverageClientSecondsText = (
            outputs.Sum(output => output.Time.TotalSeconds) / divisor).ToString("F3");

        AverageCpuSecondsText = (
            outputsWithTimeStatistics.Sum(output => output.CpuTime) / (double)timeStatisticsDivisor / 1000).ToString("F3");

        AverageLogicalReadsText = (
            outputsWithIoStatistics.Sum(output => output.LogicalReads) / (double)ioStatisticsDivisor).ToString("F1");

        AverageActualSecondsText = (
            outputsWithTimeStatistics.Sum(output => output.ElapsedTime) / (double)timeStatisticsDivisor / 1000).ToString("F3");

        ActiveThreadsText = Math.Max(0, activeThreadCount ?? threadCount - outputs.Count(output => output.Finished)).ToString();

        var total = threadCount * iterations;
        ProgressMaximum = Math.Max(1, total);
        ProgressValue = Math.Min(completed, ProgressMaximum);
    }

    private bool HasInvalidQuerySettings() =>
        HasErrors ||
        NumIterations < 1 ||
        NumThreads < 1 ||
        DelayBetweenQueries < 0;

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            if (StorageProvider == null) return;

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Settings File",
                DefaultExtension = "json",
                SuggestedFileName = "SqlQueryStressSettings.json",
                FileTypeChoices =
                [
                    new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                    new FilePickerFileType("All Files") { Patterns = ["*"] }
                ]
            });

            if (file == null) return;
            await using var stream = await file.OpenWriteAsync();
            await _settingsFileService.SaveAsync(stream);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not save settings, {exception.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        try
        {
            if (StorageProvider == null) return;

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Settings File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                    new FilePickerFileType("All Files") { Patterns = ["*"] }
                ]
            });

            if (files.Count <= 0) return;
            await using var stream = await files[0].OpenReadAsync();
            await _settingsFileService.LoadAsync(stream);
            RefreshFromService();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not load settings, {exception.Message}");
        }
    }
}
