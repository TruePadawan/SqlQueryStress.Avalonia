using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SqlQueryStress.Services.SettingsFileService;
using SqlQueryStress.ViewModels;
using TextMateSharp.Grammars;

namespace SqlQueryStress.Views;

public partial class MainView : ContentPage
{
    public MainView()
    {
        InitializeComponent();

        // Configure syntax highlighting for SQL
        var editor = this.FindControl<TextEditor>("Editor");
        if (editor != null)
        {
            var registryOptions = new RegistryOptions(ThemeName.Dark);
            var textMateInstallation = editor.InstallTextMate(registryOptions);
            var sqlLanguage = registryOptions.GetLanguageByExtension(".sql");
            var scopeName = registryOptions.GetScopeByLanguageId(sqlLanguage.Id);

            textMateInstallation.SetGrammar(scopeName);
        }
    }

    private async void OptionsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var optionsWindow = App.ServiceProvider.GetRequiredService<OptionsWindow>();
            await optionsWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Options window, {exception.Message}");
        }
    }

    private async void DatabaseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var databaseSelectWindow = App.ServiceProvider.GetRequiredService<DatabaseSelectWindow>();
            await databaseSelectWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Database Select window, {exception.Message}");
        }
    }

    private async void ExceptionsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            // TODO: Replace sample data with actual exception data from the stress test engine
            var exceptions = new ObservableCollection<ExceptionItem>
            {
                new(2, "Divide by zero error encountered."),
            };
            var exceptionsWindow = new ExceptionsWindow
            {
                DataContext = new ExceptionsViewModel(exceptions),
            };
            await exceptionsWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Exceptions window, {exception.Message}");
        }
    }

    private async void ParameterSubstitutionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var parameterSubstitutionWindow = App.ServiceProvider.GetRequiredService<ParameterSubstitutionWindow>();
            await parameterSubstitutionWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Parameter Substitution window, {exception.Message}");
        }
    }

    private async void SaveSettingsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Settings File",
                DefaultExtension = "json",
                SuggestedFileName = "SqlQueryStressSettings.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (file != null)
            {
                var settingsFileService = App.ServiceProvider.GetRequiredService<ISettingsFileService>();
                await using var stream = await file.OpenWriteAsync();
                await settingsFileService.SaveAsync(stream);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not save settings, {exception.Message}");
        }
    }

    private async void LoadSettingsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Settings File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count > 0)
            {
                var settingsFileService = App.ServiceProvider.GetRequiredService<ISettingsFileService>();
                await using var stream = await files[0].OpenReadAsync();
                await settingsFileService.LoadAsync(stream);

                if (DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.RefreshFromService();
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not load settings, {exception.Message}");
        }
    }
}