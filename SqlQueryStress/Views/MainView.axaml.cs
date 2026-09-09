using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SqlQueryStress.Views;

public partial class MainView : ContentPage
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void OptionsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
            var optionsWindow = new OptionsWindow();
            await optionsWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Options window, {exception.Message}");
        }
    }
}