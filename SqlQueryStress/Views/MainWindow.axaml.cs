using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace SqlQueryStress.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OptionsMenuItem_OnClick(object? sender, EventArgs e)
    {
        try
        {
            var mainWindow = GetTopLevel(this) as MainWindow;
            var optionsWindow = App.ServiceProvider.GetRequiredService<OptionsWindow>();
            await optionsWindow.ShowDialog(mainWindow!);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Could not open Options window, {exception.Message}");
        }
    }
}