using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.DependencyInjection;
using SqlQueryStress.Lib;
using SqlQueryStress.ViewModels;
using TextMateSharp.Grammars;

namespace SqlQueryStress.Views;

public partial class ParameterSubstitutionWindow : ModalWindow
{
    public ParameterSubstitutionWindow()
    {
        InitializeComponent();

        // Configure syntax highlighting for SQL
        var editor = this.FindControl<TextEditor>("ParameterQueryEditor");
        if (editor != null)
        {
            var registryOptions = new RegistryOptions(ThemeName.Dark);
            var textMateInstallation = editor.InstallTextMate(registryOptions);
            var sqlLanguage = registryOptions.GetLanguageByExtension(".sql");
            var scopeName = registryOptions.GetScopeByLanguageId(sqlLanguage.Id);

            textMateInstallation.SetGrammar(scopeName);
        }

        // Set editor text from ViewModel once DataContext is available
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ParameterSubstitutionViewModel vm && editor != null)
            {
                editor.Text = vm.ParameterQuery;
            }
        };
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GetColumnsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement get columns functionality
    }

    private async void DatabaseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var databaseSelectWindow = App.ServiceProvider.GetRequiredService<DatabaseSelectWindow>();
            await databaseSelectWindow.ShowDialog(this);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Database Select window, {exception.Message}");
        }
    }
}