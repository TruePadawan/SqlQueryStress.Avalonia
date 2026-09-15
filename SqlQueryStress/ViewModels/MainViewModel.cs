using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlQueryStress.Services.QuerySettingsService;
using SqlQueryStress.Services.SettingsFileService;

namespace SqlQueryStress.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IQuerySettingsService _querySettingsService;
    private readonly ISettingsFileService _settingsFileService;

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


    public MainViewModel(IQuerySettingsService querySettingsService, ISettingsFileService settingsFileService)
    {
        _querySettingsService = querySettingsService;
        _settingsFileService = settingsFileService;

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