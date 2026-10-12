using Avalonia.Interactivity;
using SqlQueryStress.Lib;

namespace SqlQueryStress.Views;

/// <summary>
/// Simple modal message dialog that mirrors the original app's MessageBox usage.
/// </summary>
public partial class MessageWindow : ModalWindow
{
    private const string DefaultTitle = "SqlQueryStress";

    public MessageWindow(string message, string title = DefaultTitle)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}