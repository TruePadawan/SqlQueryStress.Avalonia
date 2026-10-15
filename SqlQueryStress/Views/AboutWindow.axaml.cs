using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SqlQueryStress.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
