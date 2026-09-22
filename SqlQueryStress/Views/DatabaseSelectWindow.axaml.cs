using System;
using Avalonia.Interactivity;
using SqlQueryStress.Lib;
using SqlQueryStress.ViewModels;

namespace SqlQueryStress.Views;

public partial class DatabaseSelectWindow : ModalWindow
{
    private readonly DatabaseSelectViewModel _viewModel;

    public DatabaseSelectWindow(DatabaseSelectViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.ApplyChanges();
        Close();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void MainTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var success = await _viewModel.TestMainConnectionAsync();

        MainTestConnectionButton.Content = success
            ? "Connection OK"
            : "Connection Failed";
    }

    private async void ParamTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var success = await _viewModel.TestParamDatabasesAsync();
        
        ParamTestConnectionButton.Content = success
            ? "Connection OK"
            : "Connection Failed";
    }

    private async void MainDefaultDatabaseComboBox_OnDropDownOpened(object? sender, EventArgs e)
    {
        if (MainAutoRefreshCheckBox.IsChecked == true)
            await _viewModel.LoadMainDatabasesAsync();
    }

    private async void ParamDefaultDatabaseComboBox_OnDropDownOpened(object? sender, EventArgs e)
    {
        await _viewModel.LoadParamDatabasesAsync();
    }
}