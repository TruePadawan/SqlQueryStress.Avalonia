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

    private void MainTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement test connection for main load settings
    }

    private void ParamTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement test connection for parameterization settings
    }
}