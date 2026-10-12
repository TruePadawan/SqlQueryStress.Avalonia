using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.DependencyInjection;
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

        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.StorageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
                vm.ClipboardWriter = text =>
                    TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
                vm.ShowMessage = async (message, title) =>
                {
                    if (TopLevel.GetTopLevel(this) is Window window)
                        await new MessageWindow(message, title).ShowDialog(window);
                };
            }
        };
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

            if (DataContext is not MainViewModel vm)
                return;

            var exceptionsWindow = new ExceptionsWindow
            {
                DataContext = new ExceptionsViewModel(vm.Exceptions),
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
}