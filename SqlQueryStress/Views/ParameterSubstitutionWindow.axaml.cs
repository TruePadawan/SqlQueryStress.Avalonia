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
    private readonly ParameterSubstitutionViewModel _viewModel;

    public ParameterSubstitutionWindow(ParameterSubstitutionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

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
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.SaveParameters();
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