using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
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
            var optionsWindow = new OptionsWindow();
            await optionsWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Options window, {exception.Message}");
        }
    }
}