using Avalonia.Interactivity;
using SqlQueryStress.Lib;
using SqlQueryStress.ViewModels;

namespace SqlQueryStress.Views;

public partial class OptionsWindow : ModalWindow
{
    private readonly OptionsViewModel _viewModel;

    public OptionsWindow(OptionsViewModel viewModel)
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
}