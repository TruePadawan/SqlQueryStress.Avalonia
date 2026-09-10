using Avalonia.Interactivity;
using SqlQueryStress.Lib;

namespace SqlQueryStress.Views;

public partial class OptionsWindow : ModalWindow
{
    public OptionsWindow()
    {
        InitializeComponent();
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