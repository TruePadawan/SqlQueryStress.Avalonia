using Avalonia.Interactivity;
using Avalonia.Controls;

namespace SqlQueryStress.Views;

public partial class DatabaseSelectWindow : Window
{
    public DatabaseSelectWindow()
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

    private void MainTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement test connection for main load settings
    }

    private void ParamTestConnectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement test connection for parameterization settings
    }
}
