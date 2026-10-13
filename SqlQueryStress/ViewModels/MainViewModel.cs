using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using SqlQueryStress.Models;
using SqlQueryStress.Services.BenchmarkExportService;
using SqlQueryStress.Services.DbConnectionService;
using SqlQueryStress.Services.LoadEngine;
using SqlQueryStress.Services.OptionsService;
using SqlQueryStress.Services.ParameterSubstitutionService;
using SqlQueryStress.Services.QuerySettingsService;
using SqlQueryStress.Services.SettingsFileService;

namespace SqlQueryStress.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string AppTitle = "SqlQueryStress";

    private readonly IQuerySettingsService _querySettingsService;
    private readonly ISettingsFileService _settingsFileService;
    private readonly IDbConnectionService _dbConnectionService;
    private readonly IOptionsService _optionsService;
    private readonly IParameterSubstitutionService _parameterSubstitutionService;
    private readonly IBenchmarkExportService _benchmarkExportService;

    /// <summary>
    /// Set by the View after creation to enable file picker dialogs from commands.
    /// </summary>
    public IStorageProvider? StorageProvider { get; set; }

    /// <summary>
    /// Set by the View after creation to enable text/csv exports to the clipboard.
    /// </summary>
    public Func<string, Task>? ClipboardWriter { get; set; }

    /// <summary>
    /// Set by the View after creation to display modal messages (mirrors the original app's MessageBox).
    /// </summary>
    public Func<string, string, Task>? ShowMessage { get; set; }

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

    /// <summary>
    /// Deduplicated exceptions from the current run, shown in the Exceptions window.
    /// </summary>
    public ObservableCollection<ExceptionItem> Exceptions { get; } = [];

    private readonly Dictionary<string, int> _exceptionCounts = new(StringComparer.Ordinal);

    private CancellationTokenSource? _runCancellation;
    private int _runVersion;
    private Guid _testGuid;
    private DateTime _testStartTime;
    private BenchmarkResult? _lastResult;

    public MainViewModel(
        IQuerySettingsService querySettingsService,
        ISettingsFileService settingsFileService,
        IDbConnectionService dbConnectionService,
        IOptionsService optionsService,
        IParameterSubstitutionService parameterSubstitutionService,
        IBenchmarkExportService benchmarkExportService
    )
    {
        _querySettingsService = querySettingsService;
        _settingsFileService = settingsFileService;
        _dbConnectionService = dbConnectionService;
        _optionsService = optionsService;
        _parameterSubstitutionService = parameterSubstitutionService;
        _benchmarkExportService = benchmarkExportService;

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

    /// <summary>
    /// Runs the configured query on all worker threads and refreshes the displayed statistics.
    /// </summary>
    [RelayCommand]
    private async Task RunBenchmarks()
    {
        if (IsRunning || HasInvalidQuerySettings() || string.IsNullOrWhiteSpace(MainQueryDocument.Text))
            return;

        var connectionInfo = MainConnectionInfo;

        if (!await _dbConnectionService.TestConnectionAsync(connectionInfo, _optionsService.ConnectionTimeout))
        {
            await NotifyAsync("Set valid database connection information before starting a load test.", AppTitle);
            return;
        }

        IsRunning = true;
        _runCancellation = new CancellationTokenSource();

        var cancellationToken = _runCancellation.Token;
        var runVersion = Interlocked.Increment(ref _runVersion);
        var stopwatch = new Stopwatch();
        using var engine = new LoadEngine();
        var iterations = NumIterations;
        var threadCount = NumThreads;

        _testStartTime = DateTime.Now;
        _testGuid = Guid.NewGuid();

        try
        {
            // Match the original engine: tie the connection pool to the worker count
            var poolBuilder = new SqlConnectionStringBuilder(
                ConnectionStringBuilder.Build(connectionInfo, _optionsService.ConnectionTimeout))
            {
                MinPoolSize = threadCount,
                MaxPoolSize = threadCount,
                CurrentLanguage = "English"
            };
            var connectionString = poolBuilder.ConnectionString;

            // Prime the connection pool so the workers don't stall allocating connections
            using (var primingConnection = new SqlConnection(connectionString))
            {
                SqlConnection.ClearPool(primingConnection);
                primingConnection.Open();
            }

            var query = MainQueryDocument.Text;
            var queryDelay = DelayBetweenQueries;
            var collectIoStats = _optionsService.CollectIOStatistics;
            var collectTimeStats = _optionsService.CollectTimeStatistics;
            var activeMappings = _parameterSubstitutionService.ParameterMappings
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Value.ColumnName))
                .ToDictionary(mapping => mapping.Key, mapping => mapping.Value);

            ParamServer? paramServer = null;

            if (activeMappings.Count > 0)
            {
                var paramConnectionInfo = (_dbConnectionService.ShareDbSettings
                    ? _dbConnectionService.MainDbConnectionInfo
                    : _dbConnectionService.ParamDbConnectionInfo) with
                {
                    EnablePooling = _optionsService.EnableConnectionPooling
                };

                paramServer = await ParamServer.CreateAsync(
                    _parameterSubstitutionService.ParameterQuery,
                    ConnectionStringBuilder.Build(paramConnectionInfo, _optionsService.ConnectionTimeout),
                    activeMappings,
                    cancellationToken);
            }

            ResetStatistics(threadCount * iterations, threadCount);

            for (var i = 0; i < threadCount; i++)
            {
                var connection = new SqlConnection(connectionString);
                ConnectionSettingsHelper.AttachEventHandler(connection);

                var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = _optionsService.CommandTimeout
                };

                var worker = new QueryWorker(
                    connection,
                    command,
                    iterations,
                    output => PublishOutput(engine, output, stopwatch, threadCount, iterations, runVersion),
                    paramServer: paramServer,
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
            await NotifyAsync($"The load test could not be started: {exception.Message}", AppTitle);
        }
        finally
        {
            stopwatch.Stop();
            SqlConnection.ClearAllPools();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (runVersion == Volatile.Read(ref _runVersion))
                {
                    UpdateStatistics(engine.Outputs, stopwatch.Elapsed, threadCount, iterations, activeThreadCount: 0);
                    RefreshExceptions();
                    _lastResult = BuildBenchmarkResult(engine.Outputs, stopwatch.Elapsed, threadCount, iterations);
                }

                IsRunning = false;
            });

            _runCancellation.Dispose();
            _runCancellation = null;
        }
    }

    /// <summary>
    /// Requests cancellation of the current run.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _runCancellation?.Cancel();
    }

    /// <summary>
    /// Runs CHECKPOINT followed by DBCC DROPCLEANBUFFERS on the main connection.
    /// </summary>
    [RelayCommand]
    private async Task CleanBuffersAsync()
    {
        if (!await ConfirmValidConnectionAsync())
            return;

        // the original runs CHECKPOINT first, then drops clean buffers
        await _dbConnectionService.ExecuteCommandAsync(MainConnectionInfo, "CHECKPOINT");

        var success = await _dbConnectionService.ExecuteCommandAsync(MainConnectionInfo, "DBCC DROPCLEANBUFFERS");
        await NotifyAsync(success ? "Buffers cleared" : "Errors encountered", AppTitle);
    }

    /// <summary>
    /// Runs DBCC FREEPROCCACHE on the main connection.
    /// </summary>
    [RelayCommand]
    private async Task FreeCacheAsync()
    {
        if (!await ConfirmValidConnectionAsync())
            return;

        var success = await _dbConnectionService.ExecuteCommandAsync(MainConnectionInfo, "DBCC FREEPROCCACHE");
        await NotifyAsync(success ? "Cache freed" : "Errors encountered", AppTitle);
    }

    /// <summary>
    /// Exports the last benchmark result to a text file chosen by the user.
    /// </summary>
    [RelayCommand]
    private async Task SaveBenchmarkToTextAsync()
    {
        if (!TryGetLastResult(out var result))
            return;

        var file = await PickBenchmarkFileAsync("Text Files", "SqlQueryStressBenchmark.txt", "*.txt");
        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await _benchmarkExportService.ExportToTextAsync(stream, result);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not save benchmark, {exception.Message}");
            await NotifyAsync($"Error While Saving Benchmark: make sure you have write privileges to '{file.Name}'", AppTitle);
        }
    }

    /// <summary>
    /// Exports the last benchmark result to a CSV file chosen by the user.
    /// </summary>
    [RelayCommand]
    private async Task SaveBenchmarkToCsvAsync()
    {
        if (!TryGetLastResult(out var result))
            return;

        var file = await PickBenchmarkFileAsync("CSV Files", "SqlQueryStressBenchmark.csv", "*.csv");
        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await _benchmarkExportService.ExportToCsvAsync(stream, result);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not save benchmark, {exception.Message}");
            await NotifyAsync($"Error While Saving Benchmark: make sure you have write privileges to '{file.Name}'", AppTitle);
        }
    }

    /// <summary>
    /// Copies the last benchmark result to the clipboard as text.
    /// </summary>
    [RelayCommand]
    private async Task SaveBenchmarkToClipboardAsync()
    {
        if (!TryGetLastResult(out var result))
            return;

        if (ClipboardWriter is null)
        {
            await NotifyAsync("The clipboard is not available.", AppTitle);
            return;
        }

        await ClipboardWriter(_benchmarkExportService.BuildText(result));
    }

    /// <summary>
    /// The main database connection settings with the current pooling option applied.
    /// </summary>
    private DbConnectionInfo MainConnectionInfo => _dbConnectionService.MainDbConnectionInfo with
    {
        EnablePooling = _optionsService.EnableConnectionPooling
    };

    /// <summary>
    /// Returns true when no test is running and the main connection can be opened.
    /// </summary>
    private async Task<bool> ConfirmValidConnectionAsync()
    {
        if (IsRunning)
            return false;

        if (!await _dbConnectionService.TestConnectionAsync(MainConnectionInfo, _optionsService.ConnectionTimeout))
        {
            await NotifyAsync("Set valid database connection information before running this operation.", AppTitle);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the benchmark result from the last run, or prompts when there is none.
    /// </summary>
    private bool TryGetLastResult([NotNullWhen(true)] out BenchmarkResult? result)
    {
        if (_lastResult is null)
        {
            _ = NotifyAsync("Run a load test before exporting a benchmark.", AppTitle);
            result = null;
            return false;
        }

        result = _lastResult;
        return true;
    }

    /// <summary>
    /// Shows a save dialog for a benchmark file and returns the chosen file.
    /// </summary>
    private async Task<IStorageFile?> PickBenchmarkFileAsync(string displayName, string suggestedName, string pattern)
    {
        if (StorageProvider is null)
        {
            await NotifyAsync("The file picker is not available.", AppTitle);
            return null;
        }

        return await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Benchmark",
            DefaultExtension = pattern[1..],
            SuggestedFileName = suggestedName,
            FileTypeChoices =
            [
                new FilePickerFileType(displayName) { Patterns = [pattern] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });
    }

    /// <summary>
    /// Writes a message to the console and shows it to the user when a dialog is available.
    /// </summary>
    private async Task NotifyAsync(string message, string title)
    {
        Console.WriteLine($"{title}: {message}");

        if (ShowMessage is not null)
            await ShowMessage(message, title);
    }

    /// <summary>
    /// Stores a worker output and refreshes the statistics on the UI thread.
    /// </summary>
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
            if (runVersion != Volatile.Read(ref _runVersion))
                return;

            if (output.Error is not null)
                RecordException(output.Error);

            UpdateStatistics(engine.Outputs, stopwatch.Elapsed, threadCount, iterations, engine.ActiveThreadCount);
        });
    }

    /// <summary>
    /// Adds an exception to the aggregated counts, stripping the time-statistics text when enabled.
    /// </summary>
    private void RecordException(Exception exception)
    {
        var message = exception.Message;

        // Match the original: strip the parsed time-statistics text from exception messages
        if (_optionsService.CollectTimeStatistics)
        {
            var matchPos = message.IndexOf("SQL Server parse and compile time:", StringComparison.Ordinal);

            if (matchPos > -1)
                message = message[..Math.Max(0, matchPos - 2)];
        }

        if (!_exceptionCounts.TryAdd(message, 1))
            _exceptionCounts[message] += 1;
    }

    /// <summary>
    /// Clears the statistics and exceptions for a new run.
    /// </summary>
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

        _exceptionCounts.Clear();
        Exceptions.Clear();
    }

    /// <summary>
    /// Recomputes the displayed statistics from the collected worker outputs.
    /// </summary>
    private void UpdateStatistics(
        IReadOnlyList<QueryOutput> outputs,
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

    /// <summary>
    /// Rebuilds the exceptions collection from the aggregated counts.
    /// </summary>
    private void RefreshExceptions()
    {
        Exceptions.Clear();

        foreach (var pair in _exceptionCounts)
            Exceptions.Add(new ExceptionItem(pair.Value, pair.Key));
    }

    /// <summary>
    /// Builds a benchmark result from the collected worker outputs for export.
    /// </summary>
    private BenchmarkResult BuildBenchmarkResult(
        IReadOnlyList<QueryOutput> outputs,
        TimeSpan elapsed,
        int threadCount,
        int iterations)
    {
        var totalTime = 0.0;
        var totalCpu = 0.0;
        var totalElapsed = 0.0;
        var totalReads = 0.0;
        var timeMessages = 0;
        var readMessages = 0;

        foreach (var output in outputs)
        {
            totalTime += output.Time.TotalMilliseconds;

            if (output.LogicalReads > 0)
            {
                readMessages++;
                totalReads += output.LogicalReads;
            }

            if (output.ElapsedTime > 0)
            {
                timeMessages++;
                totalCpu += output.CpuTime;
                totalElapsed += output.ElapsedTime;
            }
        }

        var completed = outputs.Count;

        return new BenchmarkResult
        {
            TestId = _testGuid,
            TestStartTime = _testStartTime,
            Elapsed = elapsed,
            NumIterations = iterations,
            NumThreads = threadCount,
            DelayBetweenQueries = DelayBetweenQueries,
            CompletedIterations = completed,
            AverageClientSeconds = completed == 0 ? 0.0 : totalTime / completed / 1000,
            AverageCpuSeconds = timeMessages == 0 ? null : totalCpu / timeMessages / 1000,
            AverageActualSeconds = timeMessages == 0 ? null : totalElapsed / timeMessages / 1000,
            AverageLogicalReads = readMessages == 0 ? null : totalReads / readMessages
        };
    }

    /// <summary>
    /// Returns true when the query settings fail validation.
    /// </summary>
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
