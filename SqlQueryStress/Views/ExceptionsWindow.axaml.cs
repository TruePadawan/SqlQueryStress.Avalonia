using Avalonia.Interactivity;
using Avalonia.Controls;

namespace SqlQueryStress.Views;

public partial class ExceptionsWindow : Window
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
