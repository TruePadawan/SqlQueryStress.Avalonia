using Avalonia.Interactivity;
using SqlQueryStress.Lib;
using SqlQueryStress.ViewModels;

namespace SqlQueryStress.Views;

public partial class OptionsWindow : ModalWindow
{
    public OptionsWindow(OptionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}