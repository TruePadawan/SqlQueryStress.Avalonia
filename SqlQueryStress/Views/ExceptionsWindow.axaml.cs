using Avalonia.Interactivity;
using SqlQueryStress.Lib;

namespace SqlQueryStress.Views;

public partial class ExceptionsWindow : ModalWindow
{
    public ExceptionsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
